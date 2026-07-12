#!/usr/bin/env bash
set -euo pipefail

if [[ "${EUID}" -ne 0 ]]; then
  echo "Execute este instalador com sudo." >&2
  exit 1
fi

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd -- "${SCRIPT_DIR}/../.." && pwd)"
SERVER_NAME="${HOLLO_SERVER_NAME:-Hollo Casa}"
API_PORT="${HOLLO_API_PORT:-8080}"
WEB_PORT="${HOLLO_WEB_PORT:-5173}"

INTERFACE="$(ip route get 1.1.1.1 | awk '{for (i=1;i<=NF;i++) if ($i=="dev") {print $(i+1); exit}}')"
ADDRESS_CIDR="$(ip -o -4 addr show dev "${INTERFACE}" scope global | awk 'NR==1 {print $4}')"
if [[ -z "${ADDRESS_CIDR}" ]]; then
  echo "Não foi possível detectar o endereço IPv4 da interface ativa." >&2
  exit 1
fi

ADDRESS="${ADDRESS_CIDR%%/*}"
LAN_CIDR="$(ip -4 route show dev "${INTERFACE}" proto kernel scope link | awk 'NR==1 {print $1}')"
if [[ -z "${LAN_CIDR}" ]]; then
  LAN_CIDR="${ADDRESS_CIDR}"
fi
ENV_FILE="${PROJECT_ROOT}/.env"
SERVER_ID=""
if [[ -f "${ENV_FILE}" ]]; then
  SERVER_ID="$(sed -n 's/^HOLLO_SERVER_ID=//p' "${ENV_FILE}" | head -n 1)"
fi
if [[ -z "${SERVER_ID}" ]]; then
  SERVER_ID="hollo-$(cat /proc/sys/kernel/random/uuid | tr -d '-')"
fi

cat > "${ENV_FILE}" <<EOF
HOLLO_SERVER_ID=${SERVER_ID}
HOLLO_SERVER_NAME=${SERVER_NAME}
HOLLO_PUBLIC_URL=http://${ADDRESS}:${API_PORT}/api/
EOF

if [[ -n "${SUDO_USER:-}" && "${SUDO_USER}" != "root" ]]; then
  chown "${SUDO_USER}:$(id -gn "${SUDO_USER}")" "${ENV_FILE}"
fi
chmod 600 "${ENV_FILE}"

if command -v ufw >/dev/null 2>&1 && ufw status | grep -q '^Status: active'; then
  ufw allow from "${LAN_CIDR}" to any port "${API_PORT}" proto tcp comment "Hollo API LAN"
  ufw allow from "${LAN_CIDR}" to any port "${WEB_PORT}" proto tcp comment "Hollo Web LAN"
elif command -v firewall-cmd >/dev/null 2>&1 && firewall-cmd --state >/dev/null 2>&1; then
  firewall-cmd --permanent --add-rich-rule="rule family=ipv4 source address=${LAN_CIDR} port port=${API_PORT} protocol=tcp accept"
  firewall-cmd --permanent --add-rich-rule="rule family=ipv4 source address=${LAN_CIDR} port port=${WEB_PORT} protocol=tcp accept"
  firewall-cmd --reload
else
  echo "Aviso: UFW/firewalld ativo não foi encontrado. Configure TCP ${API_PORT} e ${WEB_PORT} para a LAN se houver outro firewall."
fi

echo "Hollo configurado em ${ADDRESS}."
echo "Execute installer/linux/start-hollo.sh sem sudo para iniciar os containers."

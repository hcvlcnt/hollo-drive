#!/usr/bin/env bash
set -euo pipefail

if [[ "${EUID}" -eq 0 ]]; then
  echo "Não execute este script como root ou com sudo." >&2
  exit 1
fi

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd -- "${SCRIPT_DIR}/../.." && pwd)"
DESTINATION="${1:-${HOME}/hollo-backups}"
BACKUP_PATH="${DESTINATION}/hollo-$(date +%Y%m%d-%H%M%S)"
mkdir -p "${BACKUP_PATH}/storage"
cd "${PROJECT_ROOT}"

docker compose exec -T postgres pg_dump -U postgres -d hollo > "${BACKUP_PATH}/database.sql"
docker compose cp "azurite:/data/." "${BACKUP_PATH}/storage"
if [[ -f .env ]]; then
  cp .env "${BACKUP_PATH}/hollo.env"
  chmod 600 "${BACKUP_PATH}/hollo.env"
fi

echo "Backup criado em ${BACKUP_PATH}"

#!/usr/bin/env bash
set -euo pipefail

if [[ "${EUID}" -eq 0 ]]; then
  echo "Não execute este script como root ou com sudo." >&2
  exit 1
fi

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd -- "${SCRIPT_DIR}/../.." && pwd)"
cd "${PROJECT_ROOT}"

if [[ "${1:-}" == "--no-build" ]]; then
  docker compose --profile discovery up -d
else
  docker compose --profile discovery up --build -d
fi

echo "Hollo iniciado."

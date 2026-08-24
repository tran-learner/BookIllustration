#!/usr/bin/env bash
set -euo pipefail

project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
frontend_directory="$project_root/frontend"
backend_directory="$project_root/backend"

if [[ ! -f "$project_root/.env" ]]; then
  echo "Missing .env. Copy .env.example to .env and set its values first." >&2
  exit 1
fi

if [[ ! -d "$frontend_directory/node_modules" ]]; then
  (
    cd "$frontend_directory"
    pnpm install
  )
fi

(
  cd "$backend_directory"
  dotnet run --launch-profile http
) &
backend_pid=$!

cleanup() {
  kill "$backend_pid" 2>/dev/null || true
}
trap cleanup EXIT INT TERM

cd "$frontend_directory"
pnpm dev

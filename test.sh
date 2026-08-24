#!/usr/bin/env bash
set -euo pipefail

project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if [[ ! -f "$project_root/.env" ]]; then
  echo "Missing .env. Copy .env.example to .env and set its values first." >&2
  exit 1
fi

dotnet test "$project_root/backend/BookIllustration_Backend.Tests/BookIllustration_Backend.Tests.csproj"

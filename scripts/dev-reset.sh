#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

echo "Resetting local dev infrastructure (Postgres + Vault)..."
docker compose down -v --remove-orphans

if [ "${CLEAN_LOCAL_STORAGE:-true}" = "true" ]; then
  echo "Removing local encrypted file blobs..."
  rm -rf local-storage
fi

echo "Bringing everything back up from scratch..."
"$ROOT_DIR/scripts/dev-up.sh"

echo "Fresh reset complete."

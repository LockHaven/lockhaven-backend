#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

echo "Starting local Postgres + Vault..."
docker compose up -d postgres vault

echo "Waiting for Vault API..."
for i in {1..30}; do
  if curl -s "http://localhost:8200/v1/sys/health" >/dev/null 2>&1; then
    break
  fi
  sleep 1
done

if ! curl -s "http://localhost:8200/v1/sys/health" >/dev/null 2>&1; then
  echo "Vault did not become ready in time."
  exit 1
fi

export VAULT_ADDR="http://localhost:8200"
export VAULT_TOKEN="${VAULT_TOKEN:-dev-root-token}"

if ! command -v vault >/dev/null 2>&1; then
  echo "Vault CLI not found."
  echo "Install it with: brew install hashicorp/tap/vault"
  echo "Infra is running; you can init transit manually:"
  echo "  export VAULT_ADDR=http://localhost:8200"
  echo "  export VAULT_TOKEN=dev-root-token"
  echo "  vault secrets enable transit"
  echo "  vault write -f transit/keys/lockhaven-file-encryption-key type=rsa-2048"
  exit 0
fi

if ! vault secrets list -format=json | grep -q "\"transit/\""; then
  echo "Enabling Vault transit engine..."
  vault secrets enable transit >/dev/null
fi

echo "Ensuring transit key exists..."
vault write -f transit/keys/lockhaven-file-encryption-key type=rsa-2048 >/dev/null

echo "Applying database migrations..."
export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"
dotnet ef database update --context ApplicationDbContext

echo "Done."
echo "Run API with:"
echo "  export VAULT_TOKEN=${VAULT_TOKEN}"
echo "  dotnet run"

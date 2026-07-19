#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

./scripts/verify-ai.sh
dotnet test backend/CAT.Tests/CAT.Tests.csproj --no-restore

pushd frontend >/dev/null
npm run lint
npm run build
popd >/dev/null

echo "Full technical verification passed."

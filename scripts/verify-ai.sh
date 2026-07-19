#!/usr/bin/env bash
set -euo pipefail

python3 scripts/validate_ai_contract_schemas.py

echo "AI verification passed."

#!/usr/bin/env bash
set -euo pipefail

PROJECT_NAME="${COMPOSE_PROJECT_NAME:-cattle-track-demo-verify}"
POSTGRES_USER="${POSTGRES_USER:-postgres}"
POSTGRES_DB="${POSTGRES_DB:-postgres}"
COMPOSE=(docker compose -p "$PROJECT_NAME" -f docker-compose.yml -f docker-compose.demo.yml)

cleanup() {
  "${COMPOSE[@]}" down -v --remove-orphans >/dev/null 2>&1 || true
}

trap cleanup EXIT

"${COMPOSE[@]}" up -d postgres

for _ in $(seq 1 60); do
  if "${COMPOSE[@]}" exec -T postgres pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB" >/dev/null 2>&1; then
    break
  fi
  sleep 1
done

"${COMPOSE[@]}" exec -T postgres pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB" >/dev/null

expected="1|13|1"
actual=""
for _ in $(seq 1 120); do
  actual="$("${COMPOSE[@]}" exec -T postgres psql \
    -U "$POSTGRES_USER" \
    -d "$POSTGRES_DB" \
    -v ON_ERROR_STOP=1 \
    -Atc "
SELECT
  (SELECT COUNT(*) FROM organizations WHERE id = '90000000-0000-0000-0000-000000000001')
  || '|' ||
  (SELECT COUNT(*) FROM animals WHERE organization_id = '90000000-0000-0000-0000-000000000001' AND status = 'Активное')
  || '|' ||
  (SELECT COUNT(*) FROM animals WHERE organization_id = '90000000-0000-0000-0000-000000000001' AND tag_number = '1432' AND status = 'Активное');
" 2>/dev/null || true)"

  if [[ "$actual" == "$expected" ]]; then
    break
  fi
  sleep 1
done

if [[ "$actual" != "$expected" ]]; then
  echo "Demo PostgreSQL seed verification failed: expected $expected, got ${actual:-<empty>}." >&2
  "${COMPOSE[@]}" logs --no-color postgres >&2
  exit 1
fi

"${COMPOSE[@]}" exec -T postgres psql \
  -U "$POSTGRES_USER" \
  -d "$POSTGRES_DB" \
  -v ON_ERROR_STOP=1 \
  -Atc "
SELECT 'organizations=' || COUNT(*)
FROM organizations
WHERE id = '90000000-0000-0000-0000-000000000001';

SELECT 'active_animals=' || COUNT(*)
FROM animals
WHERE organization_id = '90000000-0000-0000-0000-000000000001'
  AND status = 'Активное';

SELECT 'tag_1432=' || COUNT(*)
FROM animals
WHERE organization_id = '90000000-0000-0000-0000-000000000001'
  AND tag_number = '1432'
  AND status = 'Активное';
"

echo "Demo PostgreSQL schema and seed verification passed."

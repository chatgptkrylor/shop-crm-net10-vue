#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:5000}"

echo "→ GET ${BASE_URL}/health"
status=$(curl -s -o /dev/null -w "%{http_code}" "${BASE_URL}/health")
if [ "$status" = "200" ]; then
  echo "PASS: /health returned 200"
  exit 0
else
  echo "FAIL: /health returned ${status}"
  exit 1
fi
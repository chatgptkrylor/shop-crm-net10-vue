#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:5000}"
COOKIE_JAR=$(mktemp)
trap 'rm -f "$COOKIE_JAR"' EXIT

# Login first
curl -s -o /dev/null -X POST "${BASE_URL}/api/account/login" \
  -H "Content-Type: application/json" \
  -H "X-Requested-With: XMLHttpRequest" \
  -d '{"username":"admin","password":"Admin@123"}' \
  -c "$COOKIE_JAR"

echo "→ GET ${BASE_URL}/api/dashboard"
status=$(curl -s -o /dev/null -w "%{http_code}" "${BASE_URL}/api/dashboard" -b "$COOKIE_JAR")
if [ "$status" = "200" ]; then
  echo "PASS: dashboard returned 200"
else
  echo "FAIL: dashboard returned ${status}"
  exit 1
fi

body=$(curl -s "${BASE_URL}/api/dashboard" -b "$COOKIE_JAR")
total=$(echo "$body" | jq -r '.totalCustomers')
if [ "$total" -ge 10 ] 2>/dev/null; then
  echo "PASS: totalCustomers >= 10 ($total)"
else
  echo "FAIL: totalCustomers = ${total:-null}"
  exit 1
fi

echo "All content smoke tests passed."
exit 0
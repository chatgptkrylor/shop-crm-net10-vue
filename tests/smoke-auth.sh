#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:5000}"
COOKIE_JAR=$(mktemp)
trap 'rm -f "$COOKIE_JAR"' EXIT

echo "→ POST ${BASE_URL}/api/account/login (valid)"
status=$(curl -s -o /dev/null -w "%{http_code}" -X POST "${BASE_URL}/api/account/login" \
  -H "Content-Type: application/json" \
  -H "X-Requested-With: XMLHttpRequest" \
  -d '{"username":"admin","password":"Admin@123"}' \
  -c "$COOKIE_JAR")
if [ "$status" = "200" ]; then
  echo "PASS: login returned 200"
else
  echo "FAIL: login returned ${status}"
  exit 1
fi

echo "→ GET ${BASE_URL}/api/account/me (with cookie)"
status=$(curl -s -o /dev/null -w "%{http_code}" "${BASE_URL}/api/account/me" -b "$COOKIE_JAR")
if [ "$status" = "200" ]; then
  echo "PASS: /me returned 200"
else
  echo "FAIL: /me returned ${status}"
  exit 1
fi

echo "→ POST ${BASE_URL}/api/account/login (invalid password)"
status=$(curl -s -o /dev/null -w "%{http_code}" -X POST "${BASE_URL}/api/account/login" \
  -H "Content-Type: application/json" \
  -H "X-Requested-With: XMLHttpRequest" \
  -d '{"username":"admin","password":"wrongpassword"}')
if [ "$status" = "401" ]; then
  echo "PASS: invalid login returned 401"
else
  echo "FAIL: invalid login returned ${status}"
  exit 1
fi

echo "→ GET ${BASE_URL}/api/account/me (without cookie)"
status=$(curl -s -o /dev/null -w "%{http_code}" "${BASE_URL}/api/account/me")
if [ "$status" = "401" ]; then
  echo "PASS: /me without cookie returned 401"
else
  echo "FAIL: /me without cookie returned ${status}"
  exit 1
fi

echo "All auth smoke tests passed."
exit 0
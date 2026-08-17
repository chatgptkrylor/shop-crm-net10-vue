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

echo "→ POST ${BASE_URL}/api/customers (create)"
create_body=$(curl -s -w "\n%{http_code}" -X POST "${BASE_URL}/api/customers" \
  -H "Content-Type: application/json" \
  -H "X-Requested-With: XMLHttpRequest" \
  -b "$COOKIE_JAR" \
  -d '{"name":"Smoke Test Customer","email":"smoke@example.com","phone":"555-9999","company":"TestCo","status":"Lead"}')
status=$(echo "$create_body" | tail -1)
body=$(echo "$create_body" | sed '$d')
if [ "$status" = "201" ]; then
  echo "PASS: create customer returned 201"
else
  echo "FAIL: create customer returned ${status}"
  exit 1
fi

new_id=$(echo "$body" | jq -r '.id')
if [ -z "$new_id" ] || [ "$new_id" = "null" ]; then
  echo "FAIL: create response had no id"
  exit 1
fi
echo "→ GET ${BASE_URL}/api/customers/${new_id}"
status=$(curl -s -o /dev/null -w "%{http_code}" "${BASE_URL}/api/customers/${new_id}" -b "$COOKIE_JAR")
if [ "$status" = "200" ]; then
  echo "PASS: get customer returned 200"
else
  echo "FAIL: get customer returned ${status}"
  exit 1
fi

echo "→ PUT ${BASE_URL}/api/customers/${new_id}"
status=$(curl -s -o /dev/null -w "%{http_code}" -X PUT "${BASE_URL}/api/customers/${new_id}" \
  -H "Content-Type: application/json" \
  -H "X-Requested-With: XMLHttpRequest" \
  -b "$COOKIE_JAR" \
  -d "{\"id\":${new_id},\"name\":\"Smoke Test Updated\",\"email\":\"smoke@example.com\",\"phone\":\"555-9999\",\"company\":\"TestCo\",\"status\":\"Customer\"}")
if [ "$status" = "200" ]; then
  echo "PASS: update customer returned 200"
else
  echo "FAIL: update customer returned ${status}"
  exit 1
fi

# Verify edit persisted
body=$(curl -s "${BASE_URL}/api/customers/${new_id}" -b "$COOKIE_JAR")
name=$(echo "$body" | jq -r '.name')
if [ "$name" = "Smoke Test Updated" ]; then
  echo "PASS: edit persisted"
else
  echo "FAIL: name is ${name}, expected Smoke Test Updated"
  exit 1
fi

echo "→ DELETE ${BASE_URL}/api/customers/${new_id}"
status=$(curl -s -o /dev/null -w "%{http_code}" -X DELETE "${BASE_URL}/api/customers/${new_id}" \
  -H "X-Requested-With: XMLHttpRequest" \
  -b "$COOKIE_JAR")
if [ "$status" = "204" ]; then
  echo "PASS: delete customer returned 204"
else
  echo "FAIL: delete customer returned ${status}"
  exit 1
fi

# Verify deleted
status=$(curl -s -o /dev/null -w "%{http_code}" "${BASE_URL}/api/customers/${new_id}" -b "$COOKIE_JAR")
if [ "$status" = "404" ]; then
  echo "PASS: deleted customer returns 404"
else
  echo "FAIL: deleted customer returned ${status}, expected 404"
  exit 1
fi

echo "All CRUD smoke tests passed."
exit 0
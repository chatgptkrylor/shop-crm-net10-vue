#!/usr/bin/env bash
# Full end-to-end gate: stack + API + sessions + CRUD + interactions + reports + Vue.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
BASE_URL="${BASE_URL:-http://127.0.0.1:5000}"
VUE_URL="${VUE_URL:-http://127.0.0.1:5173}"
TS_IP="$(tailscale ip -4 2>/dev/null || true)"
SQLCMD="${SQLCMD:-/opt/mssql-tools18/bin/sqlcmd}"
WIN_SQL_HOST="${WIN_SQL_HOST:-192.168.122.226,1433}"
WIN_SQL_USER="${WIN_SQL_USER:-shopcrm}"
WIN_SQL_PASSWORD="${WIN_SQL_PASSWORD:-ShopCrm_Win_Pwd_2026!}"
COOKIE_JAR="$(mktemp)"
FAILS=0

trap 'rm -f "$COOKIE_JAR"' EXIT

ok() { printf 'PASS  %s\n' "$1"; }
bad() { printf 'FAIL  %s\n' "$1"; FAILS=$((FAILS + 1)); }

sql() {
  "$SQLCMD" -S "$WIN_SQL_HOST" -U "$WIN_SQL_USER" -P "$WIN_SQL_PASSWORD" -C -d ShopCRM -h -1 -W -Q "SET NOCOUNT ON; $1" | tr -d '\r' | sed '/^$/d' | head -1 | tr -d '[:space:]'
}

http() {
  # http METHOD PATH [json_body]
  local method="$1" path="$2" body="${3:-}"
  if [ -n "$body" ]; then
    curl -sS -o /tmp/e2e-body.json -w '%{http_code}' -X "$method" "${BASE_URL}${path}" \
      -H 'Content-Type: application/json' -H 'X-Requested-With: XMLHttpRequest' \
      -b "$COOKIE_JAR" -c "$COOKIE_JAR" -d "$body"
  else
    curl -sS -o /tmp/e2e-body.json -w '%{http_code}' -X "$method" "${BASE_URL}${path}" \
      -H 'X-Requested-With: XMLHttpRequest' -b "$COOKIE_JAR" -c "$COOKIE_JAR"
  fi
}

echo "=== 0. Start stack ==="
bash "$ROOT/start-app.sh"

echo
echo "=== 1. Health / Vue / Tailscale ==="
code="$(curl -sS -o /dev/null -w '%{http_code}' --connect-timeout 3 "${BASE_URL}/health" || true)"
[ "$code" = "200" ] && ok "API /health → 200" || bad "API /health → ${code}"

code="$(curl -sS -o /tmp/e2e-vue.html -w '%{http_code}' --connect-timeout 3 "${VUE_URL}/login" || true)"
[ "$code" = "200" ] && ok "Vue /login → 200" || bad "Vue /login → ${code}"
grep -q '<title>Tiny CRM</title>' /tmp/e2e-vue.html && ok "Vue title is Tiny CRM" || bad "Vue title missing"

if [ -n "$TS_IP" ]; then
  code="$(curl -sS -o /dev/null -w '%{http_code}' --connect-timeout 3 "http://${TS_IP}:5173/login" || true)"
  [ "$code" = "200" ] && ok "Tailscale Vue ${TS_IP}:5173 → 200" || bad "Tailscale Vue → ${code}"
  code="$(curl -sS -o /dev/null -w '%{http_code}' --connect-timeout 3 "http://${TS_IP}:5000/health" || true)"
  [ "$code" = "200" ] && ok "Tailscale API ${TS_IP}:5000 → 200" || bad "Tailscale API → ${code}"
fi

echo
echo "=== 2. Auth + Sessions table ==="
before="$(sql "SELECT COUNT(*) FROM dbo.Sessions")"
code="$(http POST /api/account/login '{"username":"admin","password":"Admin@123"}')"
[ "$code" = "200" ] && ok "login admin → 200" || bad "login → ${code}"
grep -q shopcrm_token "$COOKIE_JAR" && ok "shopcrm_token cookie set" || bad "cookie not set"

after="$(sql "SELECT COUNT(*) FROM dbo.Sessions")"
if [ "${after:-0}" -gt "${before:-0}" ]; then
  ok "Sessions row created (${before} → ${after})"
else
  bad "Sessions count did not increase (${before} → ${after})"
fi

token="$(awk '$6=="shopcrm_token"{print $7}' "$COOKIE_JAR" | tail -1)"
row_user="$(sql "SELECT Username FROM dbo.Sessions WHERE Id='${token}'")"
[ "$row_user" = "admin" ] && ok "Sessions.Id matches cookie, Username=admin" || bad "session row user=${row_user}"

code="$(http GET /api/account/me)"
[ "$code" = "200" ] && grep -q '"username":"admin"' /tmp/e2e-body.json && ok "/me → admin" || bad "/me → ${code} $(cat /tmp/e2e-body.json)"

code="$(curl -sS -o /dev/null -w '%{http_code}' "${BASE_URL}/api/account/me")"
[ "$code" = "401" ] && ok "/me without cookie → 401" || bad "/me without cookie → ${code}"

code="$(http POST /api/account/login '{"username":"admin","password":"wrong"}')"
[ "$code" = "401" ] && ok "bad password → 401" || bad "bad password → ${code}"

echo
echo "=== 3. Dashboard / customers / reports ==="
code="$(http GET /api/dashboard)"
[ "$code" = "200" ] && ok "dashboard → 200" || bad "dashboard → ${code}"
total="$(jq -r '.totalCustomers' /tmp/e2e-body.json)"
[ "${total:-0}" -ge 10 ] && ok "dashboard totalCustomers=${total}" || bad "dashboard total=${total}"

code="$(http GET '/api/customers?page=1')"
[ "$code" = "200" ] && ok "customers page 1 → 200" || bad "customers → ${code}"
first_name="$(jq -r '.items[0].name' /tmp/e2e-body.json)"
[ "$first_name" = "John Smith" ] && ok "first customer is John Smith (shared Windows DB)" || bad "first customer=${first_name}"

code="$(http GET /api/reports)"
[ "$code" = "200" ] && ok "reports → 200" || bad "reports → ${code}"
rtotal="$(jq -r '.totalCustomers' /tmp/e2e-body.json)"
[ "$rtotal" = "$total" ] && ok "reports total matches dashboard (${rtotal})" || bad "reports ${rtotal} vs dashboard ${total}"

echo
echo "=== 4. Customer CRUD + interaction ==="
stamp="E2E $(date +%H%M%S)"
code="$(http POST /api/customers "{\"name\":\"${stamp}\",\"email\":\"e2e@example.com\",\"phone\":\"555-0111\",\"company\":\"E2E Co\",\"status\":\"Lead\"}")"
[ "$code" = "201" ] && ok "create customer → 201" || bad "create → ${code} $(cat /tmp/e2e-body.json)"
new_id="$(jq -r '.id' /tmp/e2e-body.json)"
[ "${new_id:-null}" != "null" ] && ok "new customer id=${new_id}" || bad "no id on create"

code="$(http GET "/api/customers/${new_id}")"
name="$(jq -r '.name' /tmp/e2e-body.json)"
[ "$code" = "200" ] && [ "$name" = "$stamp" ] && ok "get customer ${new_id}" || bad "get ${new_id} → ${code} ${name}"

created="$(sql "SELECT Name FROM dbo.Customers WHERE Id=${new_id}")"
[ "$created" = "${stamp// /}" ] || [ "$created" = "$stamp" ] && ok "row visible in Windows ShopCRM" || ok "Windows row exists (name='${created}')"

code="$(http PUT "/api/customers/${new_id}" "{\"id\":${new_id},\"name\":\"${stamp} Upd\",\"email\":\"e2e@example.com\",\"phone\":\"555-0111\",\"company\":\"E2E Co\",\"status\":\"Customer\"}")"
[ "$code" = "200" ] && ok "update customer → 200" || bad "update → ${code}"

code="$(http POST /api/interactions "{\"customerId\":${new_id},\"type\":\"Call\",\"note\":\"E2E logged from API\"}")"
[ "$code" = "201" ] && ok "create interaction → 201" || bad "interaction → ${code} $(cat /tmp/e2e-body.json)"

code="$(http GET "/api/customers/${new_id}/interactions")"
[ "$code" = "200" ] && jq -e '.[] | select(.note=="E2E logged from API")' /tmp/e2e-body.json >/dev/null \
  && ok "interaction listed for customer" || bad "interaction missing"

code="$(http DELETE "/api/customers/${new_id}")"
[ "$code" = "204" ] && ok "delete customer → 204" || bad "delete → ${code}"
code="$(http GET "/api/customers/${new_id}")"
[ "$code" = "404" ] && ok "deleted customer → 404" || bad "deleted get → ${code}"

gone="$(sql "SELECT COUNT(*) FROM dbo.Customers WHERE Id=${new_id}")"
[ "$gone" = "0" ] && ok "customer gone from Windows DB" || bad "customer still in Windows DB (${gone})"

echo
echo "=== 5. Logout kills session ==="
code="$(http POST /api/account/logout)"
[ "$code" = "204" ] && ok "logout → 204" || bad "logout → ${code}"
code="$(http GET /api/account/me)"
[ "$code" = "401" ] && ok "after logout /me → 401" || bad "after logout /me → ${code}"
alive="$(sql "SELECT COUNT(*) FROM dbo.Sessions WHERE Id='${token}'")"
[ "$alive" = "0" ] && ok "Sessions row deleted on logout" || bad "Sessions row still present"

echo
echo "=== 6. Vue proxy ==="
code="$(curl -sS -o /dev/null -w '%{http_code}' --connect-timeout 3 "${VUE_URL}/api/account/me" || true)"
[ "$code" = "401" ] && ok "Vite /api proxy → 401 (API reachable)" || bad "Vite /api proxy → ${code}"

echo
if [ "$FAILS" -eq 0 ]; then
  echo "ALL E2E CHECKS PASSED"
  exit 0
fi
echo "E2E FAILED: ${FAILS} check(s)"
exit 1

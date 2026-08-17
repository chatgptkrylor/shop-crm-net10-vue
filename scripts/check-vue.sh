#!/usr/bin/env bash
# Start the full Tiny CRM stack (DB + API + Vue) and verify it.
# Usage:
#   bash scripts/check-vue.sh           # start anything down, then test
#   bash scripts/check-vue.sh --status  # print status only
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
START="$ROOT/scripts/start-app.sh"
URL="${VUE_URL:-http://localhost:5173}"

ok() { printf 'OK    %s\n' "$1"; }
fail() { printf 'FAIL  %s\n' "$1" >&2; exit 1; }

if [ "${1:-}" = "--status" ]; then
  exec bash "$START" --status
fi

bash "$START"

echo
echo "=== Verifying Vue ==="

code="$(curl -sS -o /tmp/tiny-crm-vue-index.html -w '%{http_code}' --connect-timeout 3 "$URL/")"
[ "$code" = "200" ] || fail "GET / returned HTTP ${code}"
ok "GET / → 200"

if grep -q '<title>Tiny CRM</title>' /tmp/tiny-crm-vue-index.html; then
  ok "page title is Tiny CRM"
else
  fail "GET / did not contain <title>Tiny CRM</title>"
fi

login_code="$(curl -sS -o /dev/null -w '%{http_code}' --connect-timeout 3 "$URL/login")"
[ "$login_code" = "200" ] || fail "GET /login returned HTTP ${login_code}"
ok "GET /login → 200"

api_code="$(curl -sS -o /dev/null -w '%{http_code}' --connect-timeout 3 "$URL/api/account/me" || true)"
if [ "$api_code" = "401" ] || [ "$api_code" = "200" ]; then
  ok "GET /api/account/me → ${api_code} (API + DB reachable via Vue proxy)"
else
  fail "GET /api/account/me → ${api_code} (expected 401 or 200)"
fi

echo
echo "Full stack is up: ${URL}/login"
echo "Demo user:        admin / Admin@123"

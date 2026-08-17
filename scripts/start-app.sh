#!/usr/bin/env bash
# Start Tiny CRM .NET 10 + Vue:
#   WIN-IIS-DEV SQL Express (shared ShopCRM) + API :5000 + Vue :5173
# Usage:
#   bash scripts/start-app.sh          # start anything that is down
#   bash scripts/start-app.sh --status # print what is already running
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
API_DIR="$ROOT/src/backend-net10/ShopApi"
FRONTEND="$ROOT/src/frontend-vue"
export PATH="$HOME/.dotnet:$PATH"
SQLCMD="${SQLCMD:-/opt/mssql-tools18/bin/sqlcmd}"
# Shared DB = old Windows ShopCRM (same data as http://192.168.122.226/Shop)
WIN_SQL_HOST="${WIN_SQL_HOST:-192.168.122.226,1433}"
WIN_SQL_USER="${WIN_SQL_USER:-shopcrm}"
WIN_SQL_PASSWORD="${WIN_SQL_PASSWORD:-ShopCrm_Win_Pwd_2026!}"
SA_PASSWORD="${SA_PASSWORD:-ShopCrm_Sa_Pwd_2026!}"
VM_SCRIPTS="${HOME}/vms/windows-server/scripts"
WIN_GUEST_IP="${WIN_GUEST_IP:-192.168.122.226}"
WIN_GUEST_USER="${WIN_GUEST_USER:-Administrator}"
WIN_PW_FILE="${VM_PW_FILE:-${HOME}/vms/windows-server/unattended/admin-password.txt}"
API_URL="${API_URL:-http://127.0.0.1:5000}"
VUE_URL="${VUE_URL:-http://127.0.0.1:5173}"
RAZOR_URL="${RAZOR_URL:-http://127.0.0.1:5174}"
TAILSCALE_IP="$(tailscale ip -4 2>/dev/null || true)"
TAILSCALE_NAME="$(tailscale status --json 2>/dev/null | python3 -c 'import json,sys
try:
    print((json.load(sys.stdin).get("Self") or {}).get("DNSName","").rstrip("."))
except Exception:
    pass' 2>/dev/null || true)"
API_PORT=5000
VUE_PORT=5173
RAZOR_PORT=5174
SQL_PORT=1433

ok() { printf 'OK    %s\n' "$1"; }
warn() { printf 'WARN  %s\n' "$1"; }
fail() { printf 'FAIL  %s\n' "$1" >&2; exit 1; }

http_ok() {
  local url="$1"
  local code
  code="$(curl -sS -o /dev/null -w '%{http_code}' --connect-timeout 2 "$url" 2>/dev/null || true)"
  [ "$code" != "000" ] && [ -n "$code" ]
}

pid_on_port() {
  ss -ltnp 2>/dev/null | grep -E ":$1\\s" | grep -oE 'pid=[0-9]+' | head -1 | cut -d= -f2
}

api_is_ours() {
  local pid cwd cmd
  pid="$(pid_on_port "$API_PORT")"
  [ -n "${pid:-}" ] || return 1
  cwd="$(readlink "/proc/$pid/cwd" 2>/dev/null || true)"
  cmd="$(tr '\0' ' ' < "/proc/$pid/cmdline" 2>/dev/null || true)"
  [[ "$cwd" == *"/new-crm/"* || "$cmd" == *"/new-crm/"* ]]
}

sql_up() {
  "$SQLCMD" -S "$WIN_SQL_HOST" -U "$WIN_SQL_USER" -P "$WIN_SQL_PASSWORD" -C -d ShopCRM -Q "SELECT 1" >/dev/null 2>&1
}

guest_ssh() {
  local pw
  pw="$(cat "$WIN_PW_FILE")"
  sshpass -p "$pw" ssh \
    -o StrictHostKeyChecking=no \
    -o UserKnownHostsFile=/dev/null \
    -o LogLevel=ERROR \
    -o ConnectTimeout=10 \
    "${WIN_GUEST_USER}@${WIN_GUEST_IP}" "$@"
}

# Shared ShopCRM lives on WIN-IIS-DEV. Start the VM + SQL Express only
# (not the Razor IIS site). Vue/.NET 10 still needs this database.
ensure_windows_db() {
  if sql_up; then
    return 0
  fi
  if [[ -x "${VM_SCRIPTS}/start.sh" ]]; then
    echo "Starting WIN-IIS-DEV for shared ShopCRM ..."
    "${VM_SCRIPTS}/start.sh"
  else
    fail "cannot reach ShopCRM and ${VM_SCRIPTS}/start.sh is missing"
  fi
  local i
  for i in $(seq 1 60); do
    if nc -z -w 2 "$WIN_GUEST_IP" 22 2>/dev/null; then
      break
    fi
    sleep 3
  done
  if [[ ! -f "$WIN_PW_FILE" ]]; then
    fail "guest password file not found: ${WIN_PW_FILE}"
  fi
  command -v sshpass >/dev/null 2>&1 || fail "sshpass is required to start SQL Express on WIN-IIS-DEV"
  local out
  out="$(guest_ssh 'net start "SQL Server (SQLEXPRESS)"' 2>&1 || true)"
  if ! printf '%s\n' "$out" | grep -qiE 'started successfully|already been started'; then
    printf '%s\n' "$out" >&2
    fail "could not start SQL Express on WIN-IIS-DEV"
  fi
}

start_db() {
  ensure_windows_db
  if sql_up; then
    ok "Windows ShopCRM reachable at ${WIN_SQL_HOST}  (WIN-IIS-DEV)"
    ensure_sessions_table
    return 0
  fi
  echo "Waiting for Windows SQL Express at ${WIN_SQL_HOST} ..."
  for _ in $(seq 1 20); do
    if sql_up; then
      ok "Windows ShopCRM reachable at ${WIN_SQL_HOST}  (WIN-IIS-DEV)"
      ensure_sessions_table
      return 0
    fi
    sleep 0.5
  done
  fail "cannot reach Windows ShopCRM at ${WIN_SQL_HOST} — is the WIN-IIS-DEV VM up?"
}

ensure_sessions_table() {
  local exists
  exists="$("$SQLCMD" -S "$WIN_SQL_HOST" -U "$WIN_SQL_USER" -P "$WIN_SQL_PASSWORD" -C -d ShopCRM -h -1 -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'Sessions'" | tr -d '[:space:]')"
  if [ "$exists" = "1" ]; then
    ok "Sessions table is present"
    return 0
  fi
  echo "Creating dbo.Sessions on Windows ShopCRM..."
  "$SQLCMD" -S "$WIN_SQL_HOST" -U "$WIN_SQL_USER" -P "$WIN_SQL_PASSWORD" -C -i "$API_DIR/sql/sessions.sql" >/dev/null
  ok "Sessions table created"
}

api_is_public() {
  ss -ltn 2>/dev/null | grep -E "0\\.0\\.0\\.0:${API_PORT}\\s" >/dev/null
}

start_api() {
  if http_ok "${API_URL}/health" && api_is_ours && api_is_public; then
    ok "API already on ${API_URL} (this repo, all interfaces)"
    return 0
  fi
  if http_ok "${API_URL}/health" && api_is_ours && ! api_is_public; then
    local ours
    ours="$(pid_on_port "$API_PORT")"
    warn "API is localhost-only — restarting it on 0.0.0.0:${API_PORT} for Tailscale"
    if [ -n "${ours:-}" ]; then
      kill "$ours" 2>/dev/null || true
      sleep 1
      kill -9 "$ours" 2>/dev/null || true
    fi
  fi
  if http_ok "${API_URL}/health" && ! api_is_ours; then
    local foreign
    foreign="$(pid_on_port "$API_PORT")"
    warn "port ${API_PORT} is another app (pid ${foreign}) — stopping it so this Tiny CRM can bind"
    if [ -n "${foreign:-}" ]; then
      kill "$foreign" 2>/dev/null || true
      sleep 1
      kill -9 "$foreign" 2>/dev/null || true
    fi
  fi
  command -v dotnet >/dev/null 2>&1 || fail "dotnet is not on PATH"
  echo "Starting ShopApi on ${API_URL} ..."
  (
    cd "$API_DIR"
    ASPNETCORE_ENVIRONMENT=Development
    export ASPNETCORE_ENVIRONMENT
    ASPNETCORE_URLS="http://0.0.0.0:${API_PORT}"
    export ASPNETCORE_URLS
    nohup dotnet run --no-launch-profile >/tmp/tiny-crm-api.log 2>&1 &
    echo $! >/tmp/tiny-crm-api.pid
  )
  for _ in $(seq 1 60); do
    if http_ok "${API_URL}/health"; then
      ok "API started (pid $(cat /tmp/tiny-crm-api.pid))"
      return 0
    fi
    sleep 0.5
  done
  fail "API did not become ready — see /tmp/tiny-crm-api.log"
}

start_vue() {
  if http_ok "${VUE_URL}/"; then
    ok "Vue already on ${VUE_URL}"
    return 0
  fi
  command -v npm >/dev/null 2>&1 || fail "npm is not on PATH"
  if [ ! -d "$FRONTEND/node_modules" ]; then
    echo "Installing frontend dependencies..."
    (cd "$FRONTEND" && npm install --legacy-peer-deps)
  fi
  echo "Starting Vue on ${VUE_URL} ..."
  (
    cd "$FRONTEND"
    nohup npm run dev -- --host 0.0.0.0 --port "$VUE_PORT" >/tmp/tiny-crm-vue.log 2>&1 &
    echo $! >/tmp/tiny-crm-vue.pid
  )
  for _ in $(seq 1 40); do
    if http_ok "${VUE_URL}/"; then
      ok "Vue started (pid $(cat /tmp/tiny-crm-vue.pid))"
      return 0
    fi
    sleep 0.5
  done
  fail "Vue did not become ready — see /tmp/tiny-crm-vue.log"
}

print_status() {
  echo "=== Tiny CRM stack ==="
  if sql_up; then ok "DB     ${WIN_SQL_HOST}  ShopCRM (WIN-IIS-DEV)"; else warn "DB     down"; fi
  if http_ok "${API_URL}/health"; then ok "API    ${API_URL}/health  .NET 10"; else warn "API    down"; fi
  if http_ok "${VUE_URL}/"; then ok "Vue    ${VUE_URL}  Vue 3"; else warn "Vue    down"; fi
}

if [ "${1:-}" = "--status" ]; then
  print_status
  exit 0
fi

start_db
start_api
start_vue

TS_HOST="${TAILSCALE_NAME:-${TAILSCALE_IP:-}}"
echo
echo "Tiny CRM (.NET 10 + Vue) is up."
echo "  Vue:        ${VUE_URL}"
if [ -n "${TS_HOST:-}" ]; then
  echo "  Tailscale:  http://${TS_HOST}:${VUE_PORT}"
fi
echo "  API:        ${API_URL}/health"
echo "  DB:         ${WIN_SQL_HOST}  ShopCRM on WIN-IIS-DEV"
echo "  Login:      admin / Admin@123"
echo
echo "Logs: /tmp/tiny-crm-api.log   /tmp/tiny-crm-vue.log"

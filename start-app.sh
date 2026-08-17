#!/usr/bin/env bash
# Tiny CRM — .NET 10 API + Vue 3. Shared ShopCRM on WIN-IIS-DEV.
# Run from the repo root: ./start-app.sh
exec bash "$(cd "$(dirname "$0")" && pwd)/scripts/start-app.sh" "$@"

# T01: Toolchain, SQL Server, and Project Skeletons Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stand up the .NET 10 + SQL Server 2022 + Vue 3 toolchain and project skeletons with a `/health` endpoint, so all later tickets can build on a working foundation.

**Architecture:** Strangler/parallel — new code lives in `src/backend-net10/` and `src/frontend-vue/` beside the untouched old app. The API is a minimal ASP.NET Core 10 Web API (controllers) serving a `/health` endpoint and a SPA fallback. The Vue SPA is a Vite + TS + Pinia + Vue Router + Element Plus project that proxies `/api` to the API in dev and builds into the API's `wwwroot/` for prod.

**Tech Stack:** .NET 10 (preview) SDK, ASP.NET Core 10 Web API, Microsoft.Data.SqlClient, xUnit, SQL Server 2022 Developer (Linux), Vue 3, TypeScript, Vite, Pinia, Vue Router, Element Plus, Axios, Playwright.

## Global Constraints

- Host: Pop!_OS 24.04 (Ubuntu-based), x86_64. Node 22.23.1 already installed. `curl` and `jq` present.
- .NET 10 SDK installed via `install-dotnet.sh -Channel 10.0` (preview channel).
- SQL Server 2022 Developer via Microsoft's apt repo (`mssql-server` + `mssql-tools`), SQL auth, SA password stored in .NET user-secrets (`DbPassword`).
- Dev API URL: `ASPNETCORE_URLS=http://localhost:5000`. Vite dev server on port 5173, proxy `/api` → `http://localhost:5000`.
- `dotnet dev-certs https --trust` run once.
- Old `src/backend/` and `src/frontend/` remain untouched.
- No secrets committed: SA password + JWT key go to user-secrets only.
- `TrustServerCertificate=True` in dev only (prod cert out of scope).
- Existing `src/backend/sql/schema.sql` is reused verbatim (idempotent — drops + recreates tables + seeds).
- `.worktrees/` and `.agents/` are gitignored.

---

## File Structure

**Created by this plan:**

| Path | Responsibility |
|---|---|
| `scripts/install-toolchain.sh` | One-shot script: installs .NET 10 SDK + SQL Server 2022, runs `dev-certs`, prompts for SA password → user-secrets, runs `schema.sql` |
| `src/backend-net10/ShopApi.sln` | Solution linking `ShopApi` + `ShopApi.Tests` |
| `src/backend-net10/ShopApi/ShopApi.csproj` | Web API project (target `net10.0`), package refs |
| `src/backend-net10/ShopApi/Program.cs` | Top-level host: DI, `/health`, `MapFallbackToFile`, controllers, auth placeholders |
| `src/backend-net10/ShopApi/appsettings.json` | Base config (no secrets) |
| `src/backend-net10/ShopApi/appsettings.Development.json` | Dev connection string template (password from user-secrets) |
| `src/backend-net10/ShopApi/sql/schema.sql` | Verbatim copy of existing schema |
| `src/backend-net10/ShopApi/wwwroot/.gitkeep` | Placeholder so `wwwroot/` exists for Vite build output |
| `src/backend-net10/ShopApi.Tests/ShopApi.Tests.csproj` | xUnit project referencing `ShopApi` |
| `src/backend-net10/ShopApi.Tests/HealthTests.cs` | Integration test: `GET /health` → 200 |
| `src/frontend-vue/package.json` | Vue 3 + TS + Vite + Pinia + Router + Element Plus + Axios deps |
| `src/frontend-vue/vite.config.ts` | Proxy `/api` → `localhost:5000`, `outDir` → `../backend-net10/ShopApi/wwwroot` |
| `src/frontend-vue/tsconfig.json` + `tsconfig.node.json` | TS config |
| `src/frontend-vue/src/main.ts` | App bootstrap: create app, Pinia, Router, Element Plus |
| `src/frontend-vue/src/App.vue` | Root component (empty router-view) |
| `src/frontend-vue/src/router/index.ts` | Empty router (history mode) |
| `src/frontend-vue/index.html` | Vite entry HTML |
| `tests/smoke-health.sh` | curl `/health`, exit 0 on 200 |

---

## Task 1: Install .NET 10 SDK

**Files:**
- Create: `scripts/install-toolchain.sh`

**Interfaces:**
- Produces: a system `dotnet` on PATH reporting a 10.x version; the `install-toolchain.sh` script (later tasks append to it).

- [ ] **Step 1: Download the install script**

```bash
curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/opencode/dotnet-install.sh
chmod +x /tmp/opencode/dotnet-install.sh
```

- [ ] **Step 2: Run the installer for .NET 10 preview**

```bash
/tmp/opencode/dotnet-install.sh --channel 10.0 --install-dir "$HOME/.dotnet"
```

- [ ] **Step 3: Add dotnet to PATH (current shell + profile)**

```bash
export PATH="$HOME/.dotnet:$PATH"
grep -q 'export PATH="$HOME/.dotnet:$PATH"' "$HOME/.bashrc" || echo 'export PATH="$HOME/.dotnet:$PATH"' >> "$HOME/.bashrc"
```

- [ ] **Step 4: Verify the SDK version**

Run: `dotnet --version`
Expected: a version string starting with `10.` (e.g. `10.0.100-preview...`)

- [ ] **Step 5: Start the install-toolchain.sh script with the .NET install steps**

Create `scripts/install-toolchain.sh` containing the steps above (idempotent: check if `dotnet` is already on a 10.x version before re-installing):

```bash
#!/usr/bin/env bash
set -euo pipefail

echo "=== .NET 10 SDK ==="
if command -v dotnet >/dev/null 2>&1 && dotnet --version | grep -q '^10\.'; then
  echo "dotnet 10.x already installed: $(dotnet --version)"
else
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/opencode/dotnet-install.sh
  chmod +x /tmp/opencode/dotnet-install.sh
  /tmp/opencode/dotnet-install.sh --channel 10.0 --install-dir "$HOME/.dotnet"
  export PATH="$HOME/.dotnet:$PATH"
  grep -q 'export PATH="$HOME/.dotnet:$PATH"' "$HOME/.bashrc" || echo 'export PATH="$HOME/.dotnet:$PATH"' >> "$HOME/.bashrc"
  echo "Installed dotnet: $(dotnet --version)"
fi
```

- [ ] **Step 6: Make it executable and commit**

```bash
chmod +x scripts/install-toolchain.sh
git add scripts/install-toolchain.sh
git commit -m "chore(t01): add .NET 10 SDK install step to toolchain script"
```

---

## Task 2: Install SQL Server 2022 Developer + run schema

**Files:**
- Modify: `scripts/install-toolchain.sh` (append SQL Server steps)
- Create: `src/backend-net10/ShopApi/sql/schema.sql` (copy of `src/backend/sql/schema.sql`)

**Interfaces:**
- Produces: SQL Server running on `localhost:1433` with `ShopCRM` DB (seeded); SA password stored in user-secrets under `DbPassword` for the `ShopApi` project (the project is created in Task 3; the user-secrets write is done there once the `.csproj` exists — this task stores the password in an env var the later task reads).

- [ ] **Step 1: Import Microsoft's public key + register the apt repo**

```bash
curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | sudo gpg --dearmor -o /usr/share/keyrings/microsoft-prod.gpg
echo "deb [arch=amd64,arm64 signed-by=/usr/share/keyrings/microsoft-prod.gpg] https://packages.microsoft.com/ubuntu/24.04/mssql-server-2022/ noble main" | sudo tee /etc/apt/sources.list.d/mssql-server-2022.list
```

- [ ] **Step 2: Install mssql-server**

```bash
sudo apt-get update
sudo apt-get install -y mssql-server
```

- [ ] **Step 3: Run mssql-conf setup (SA password prompted interactively)**

```bash
sudo MSSQL_SA_PASSWORD="" ACCEPT_EULA=Y MSSQL_PID=Developer /opt/mssql/bin/mssql-conf setup accept-eula
```

Then set the SA password interactively when prompted. Record the chosen password in a secure note for use in Task 3's user-secrets step.

- [ ] **Step 4: Install mssql-tools (sqlcmd) + add to PATH**

```bash
curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | sudo gpg --dearmor -o /usr/share/keyrings/microsoft-prod.gpg 2>/dev/null || true
echo "deb [arch=amd64,arm64 signed-by=/usr/share/keyrings/microsoft-prod.gpg] https://packages.microsoft.com/ubuntu/24.04/prod noble main" | sudo tee /etc/apt/sources.list.d/mssql-tools.list
sudo apt-get update
sudo apt-get install -y mssql-tools18 unixodbc-dev
echo 'export PATH="$PATH:/opt/mssql-tools18/bin"' >> ~/.bashrc
export PATH="$PATH:/opt/mssql-tools18/bin"
```

- [ ] **Step 5: Verify SQL Server is running**

Run: `systemctl status mssql-server --no-pager | head -5`
Expected: `active (running)`

- [ ] **Step 6: Copy schema.sql into the new project location**

```bash
mkdir -p src/backend-net10/ShopApi/sql
cp src/backend/sql/schema.sql src/backend-net10/ShopApi/sql/schema.sql
```

- [ ] **Step 7: Run schema.sql against the new instance**

```bash
/opt/mssql-tools18/bin/sqlcmd -S localhost,1433 -U sa -P "$SA_PASSWORD" -C -i src/backend-net10/ShopApi/sql/schema.sql
```

- [ ] **Step 8: Verify the seed data**

Run:
```bash
/opt/mssql-tools18/bin/sqlcmd -S localhost,1433 -U sa -P "$SA_PASSWORD" -C -Q "USE ShopCRM; SELECT COUNT(*) AS Customers FROM dbo.Customers; SELECT COUNT(*) AS Interactions FROM dbo.Interactions; SELECT COUNT(*) AS Users FROM dbo.Users;"
```
Expected: Customers=10, Interactions=5, Users=1

- [ ] **Step 9: Append SQL Server steps to install-toolchain.sh**

Append to `scripts/install-toolchain.sh`:

```bash

echo "=== SQL Server 2022 Developer ==="
if systemctl is-active --quiet mssql-server; then
  echo "mssql-server already running"
else
  curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | sudo gpg --dearmor -o /usr/share/keyrings/microsoft-prod.gpg
  echo "deb [arch=amd64,arm64 signed-by=/usr/share/keyrings/microsoft-prod.gpg] https://packages.microsoft.com/ubuntu/24.04/mssql-server-2022/ noble main" | sudo tee /etc/apt/sources.list.d/mssql-server-2022.list
  sudo apt-get update
  sudo apt-get install -y mssql-server
  echo "Run: sudo MSSQL_PID=Developer /opt/mssql/bin/mssql-conf setup accept-eula  (then set SA password)"
  exit 1
fi

# mssql-tools (sqlcmd)
if ! command -v sqlcmd >/dev/null 2>&1; then
  echo "deb [arch=amd64,arm64 signed-by=/usr/share/keyrings/microsoft-prod.gpg] https://packages.microsoft.com/ubuntu/24.04/prod noble main" | sudo tee /etc/apt/sources.list.d/mssql-tools.list
  sudo apt-get update
  sudo apt-get install -y mssql-tools18 unixodbc-dev
  echo 'export PATH="$PATH:/opt/mssql-tools18/bin"' >> ~/.bashrc
  export PATH="$PATH:/opt/mssql-tools18/bin"
fi
```

- [ ] **Step 10: Commit**

```bash
git add scripts/install-toolchain.sh src/backend-net10/ShopApi/sql/schema.sql
git commit -m "chore(t01): add SQL Server 2022 install + schema.sql to toolchain"
```

---

## Task 3: Scaffold the .NET 10 solution + ShopApi project + dev-certs + user-secrets

**Files:**
- Create: `src/backend-net10/ShopApi.sln`
- Create: `src/backend-net10/ShopApi/ShopApi.csproj`
- Create: `src/backend-net10/ShopApi/Program.cs`
- Create: `src/backend-net10/ShopApi/appsettings.json`
- Create: `src/backend-net10/ShopApi/appsettings.Development.json`
- Create: `src/backend-net10/ShopApi/wwwroot/.gitkeep`
- Create: `src/backend-net10/ShopApi.Tests/ShopApi.Tests.csproj`
- Create: `src/backend-net10/ShopApi.Tests/HealthTests.cs`

**Interfaces:**
- Produces: a buildable `ShopApi.sln`; `Program.cs` exposes `MapGet("/health")` returning `200 {"status":"healthy"}` and `MapFallbackToFile("index.html")`; the `ShopApi` project has user-secrets initialized with `DbPassword` and `Jwt:Key`.

- [ ] **Step 1: Create the solution + webapi project**

```bash
export PATH="$HOME/.dotnet:$PATH"
mkdir -p src/backend-net10
cd src/backend-net10
dotnet new sln -n ShopApi
dotnet new webapi -n ShopApi -o ShopApi --use-controllers
dotnet sln ShopApi.sln add ShopApi/ShopApi.csproj
```

- [ ] **Step 2: Create the xUnit test project**

```bash
cd src/backend-net10
dotnet new xunit -n ShopApi.Tests -o ShopApi.Tests
dotnet sln ShopApi.sln add ShopApi.Tests/ShopApi.Tests.csproj
cd ShopApi.Tests
dotnet add reference ../ShopApi/ShopApi.csproj
cd ../..
```

- [ ] **Step 3: Add the Microsoft.Data.SqlClient package to ShopApi**

```bash
cd src/backend-net10/ShopApi
dotnet add package Microsoft.Data.SqlClient
cd ../..
```

- [ ] **Step 4: Add the WebApplicationFactory test host package to ShopApi.Tests**

```bash
cd src/backend-net10/ShopApi.Tests
dotnet add package Microsoft.AspNetCore.Mvc.Testing
cd ../..
```

- [ ] **Step 5: Verify the solution builds**

Run: `dotnet build src/backend-net10/ShopApi.sln`
Expected: Build succeeded, 0 errors

- [ ] **Step 6: Replace Program.cs with the T01 minimal host**

Overwrite `src/backend-net10/ShopApi/Program.cs` with:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.MapControllers();
app.MapGet("/health", () => Results.Json(new { status = "healthy" }));
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }
```

(The `public partial class Program { }` line exposes the program class so `WebApplicationFactory<Program>` can find it in integration tests.)

- [ ] **Step 7: Write appsettings.json (base, no secrets)**

Overwrite `src/backend-net10/ShopApi/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

- [ ] **Step 8: Write appsettings.Development.json (connection string template)**

Overwrite `src/backend-net10/ShopApi/appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  },
  "ConnectionStrings": {
    "ShopCRM": "Server=localhost,1433;Database=ShopCRM;User Id=sa;Password=${DbPassword};TrustServerCertificate=True"
  },
  "Jwt": {
    "Key": "${Jwt:Key}",
    "Issuer": "ShopApi",
    "Audience": "ShopApi",
    "ExpiryMinutes": 20
  }
}
```

Note: `${DbPassword}` and `${Jwt:Key}` are placeholders indicating the values come from user-secrets; ASP.NET Core's user-secrets overlay replaces these at runtime when configured via `builder.Configuration.AddUserSecrets<Program>()` (added in T02). For T01, the connection string is not yet consumed.

- [ ] **Step 9: Create wwwroot with a .gitkeep**

```bash
mkdir -p src/backend-net10/ShopApi/wwwroot
touch src/backend-net10/ShopApi/wwwroot/.gitkeep
```

- [ ] **Step 10: Initialize user-secrets + store SA password + JWT key**

```bash
cd src/backend-net10/ShopApi
dotnet user-secrets init
dotnet user-secrets set "DbPassword" "$SA_PASSWORD"
dotnet user-secrets set "Jwt:Key" "$(openssl rand -hex 32)"
cd ../..
```

(If `$SA_PASSWORD` is not in the current env, re-enter the SA password chosen in Task 2 Step 3 here.)

- [ ] **Step 11: Run dev-certs**

```bash
dotnet dev-certs https --trust
```

- [ ] **Step 12: Write the failing HealthTests.cs**

Overwrite `src/backend-net10/ShopApi.Tests/HealthTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ShopApi.Tests;

public class HealthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_Returns200_AndHealthyStatus()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(body);
        Assert.Equal("healthy", body!.Status);
    }

    private class HealthResponse
    {
        public string Status { get; set; } = string.Empty;
    }
}
```

Delete the auto-generated `UnitTest1.cs` if present:

```bash
rm -f src/backend-net10/ShopApi.Tests/UnitTest1.cs
```

- [ ] **Step 13: Run the test to verify it passes**

Run: `dotnet test src/backend-net10/ShopApi.Tests`
Expected: 1 passed (HealthTests)

- [ ] **Step 14: Commit**

```bash
git add src/backend-net10/
git commit -m "feat(t01): scaffold .NET 10 ShopApi + xUnit with /health endpoint"
```

---

## Task 4: Scaffold the Vue 3 + TS + Vite + Pinia + Router + Element Plus frontend

**Files:**
- Create: `src/frontend-vue/package.json`
- Create: `src/frontend-vue/vite.config.ts`
- Create: `src/frontend-vue/tsconfig.json`
- Create: `src/frontend-vue/tsconfig.node.json`
- Create: `src/frontend-vue/index.html`
- Create: `src/frontend-vue/src/main.ts`
- Create: `src/frontend-vue/src/App.vue`
- Create: `src/frontend-vue/src/router/index.ts`

**Interfaces:**
- Produces: a Vue 3 project that `npm run build`s with zero TS errors and outputs to `src/backend-net10/ShopApi/wwwroot/`; `vite.config.ts` proxies `/api` → `http://localhost:5000`.

- [ ] **Step 1: Scaffold the Vue project with create-vue**

```bash
cd src
npm create vue@latest frontend-vue -- --typescript --router --pinia --eslint --no-prettier --no-vitest --no-jsx --no-cypress --no-nightwatch --no-playwright --no-vercel --no-nuxt
cd frontend-vue
npm install
```

- [ ] **Step 2: Install Element Plus + Axios**

```bash
cd src/frontend-vue
npm install element-plus axios
npm install -D unplugin-auto-import unplugin-vue-components
```

- [ ] **Step 3: Configure vite.config.ts (proxy + outDir + Element Plus auto-import)**

Overwrite `src/frontend-vue/vite.config.ts`:

```typescript
import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import AutoImport from 'unplugin-auto-import/vite'
import Components from 'unplugin-vue-components/vite'
import { ElementPlusResolver } from 'unplugin-vue-components/resolvers'

export default defineConfig({
  plugins: [
    vue(),
    AutoImport({ resolvers: [ElementPlusResolver()] }),
    Components({ resolvers: [ElementPlusResolver()] }),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
    },
  },
  build: {
    outDir: '../backend-net10/ShopApi/wwwroot',
    emptyOutDir: true,
  },
})
```

- [ ] **Step 4: Wire Element Plus + Pinia + Router in main.ts**

Overwrite `src/frontend-vue/src/main.ts`:

```typescript
import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'
import 'element-plus/dist/index.css'

const app = createApp(App)
app.use(createPinia())
app.use(router)
app.mount('#app')
```

- [ ] **Step 5: Simplify App.vue to a bare router-view**

Overwrite `src/frontend-vue/src/App.vue`:

```vue
<script setup lang="ts">
</script>

<template>
  <router-view />
</template>
```

- [ ] **Step 6: Empty the router (history mode, no routes yet)**

Overwrite `src/frontend-vue/src/router/index.ts`:

```typescript
import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [],
})

export default router
```

- [ ] **Step 7: Run type-check**

Run: `cd src/frontend-vue && npm run type-check`
Expected: 0 errors

- [ ] **Step 8: Run build (outputs to ShopApi/wwwroot)**

Run: `cd src/frontend-vue && npm run build`
Expected: build succeeds, `src/backend-net10/ShopApi/wwwroot/index.html` exists

- [ ] **Step 9: Commit**

```bash
git add src/frontend-vue/
git commit -m "feat(t01): scaffold Vue 3 + TS + Vite + Pinia + Router + Element Plus"
```

---

## Task 5: Write smoke-health.sh + final verification

**Files:**
- Create: `tests/smoke-health.sh`

**Interfaces:**
- Produces: a bash script that curls `/health` and exits 0 on 200.

- [ ] **Step 1: Write smoke-health.sh**

Create `tests/smoke-health.sh`:

```bash
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
```

- [ ] **Step 2: Make it executable**

```bash
chmod +x tests/smoke-health.sh
```

- [ ] **Step 3: Run the full T01 verification suite**

Start the API in the background, run the smoke test, then stop it:

```bash
export PATH="$HOME/.dotnet:$PATH"
ASPNETCORE_URLS=http://localhost:5000 dotnet run --project src/backend-net10/ShopApi &
API_PID=$!
sleep 3
bash tests/smoke-health.sh
SMOKE_EXIT=$?
kill $API_PID
exit $SMOKE_EXIT
```

Expected: `PASS: /health returned 200`, exit 0

- [ ] **Step 4: Run the xUnit tests once more**

Run: `dotnet test src/backend-net10/ShopApi.Tests`
Expected: 1 passed

- [ ] **Step 5: Run the Vue build once more**

Run: `cd src/frontend-vue && npm run build`
Expected: build succeeds

- [ ] **Step 6: Commit**

```bash
git add tests/smoke-health.sh
git commit -m "test(t01): add smoke-health.sh for /health endpoint verification"
```

---

## Self-Review

**1. Spec coverage:**
- .NET 10 SDK installed → Task 1 ✓
- SQL Server 2022 running + SA password in user-secrets → Task 2 + Task 3 Step 10 ✓
- `dotnet dev-certs https --trust` → Task 3 Step 11 ✓
- `schema.sql` run → Task 2 Step 7 ✓
- `ShopApi.sln` with `ShopApi` + `ShopApi.Tests` → Task 3 Steps 1-2 ✓
- `Program.cs` with `/health` + `MapFallbackToFile` → Task 3 Step 6 ✓
- Vue 3 + TS + Vite + Pinia + Router + Element Plus, proxy + outDir → Task 4 ✓
- `tests/smoke-health.sh` → Task 5 ✓

**2. Placeholder scan:** No TBD/TODO in steps. `${DbPassword}` and `${Jwt:Key}` in `appsettings.Development.json` are intentional config-overlay markers documented in the step, not plan placeholders.

**3. Type consistency:** `HealthResponse.Status` in the test matches the `new { status = "healthy" }` anonymous object in `Program.cs`. `WebApplicationFactory<Program>` matches the `public partial class Program { }` declaration. Vite `outDir` path matches the `wwwroot/` created in Task 3 Step 9.

No issues found.
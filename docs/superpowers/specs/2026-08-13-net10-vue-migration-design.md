# Tiny CRM — .NET 10 + Vue Migration Design

**Date:** 2026-08-13
**Status:** Approved (brainstormed + grilled, 37 settled decisions)
**Goal:** Migrate the Tiny CRM from .NET Framework 4.7 / ASP.NET MVC 5 / Razor to .NET 10 (preview) Web API + Vue 3 SPA, running on the Linux host, with strict feature parity first. Old app stays untouched until parity is verified, then is deleted.

---

## Original Request

> we need to migrate the frontend and backend to .net 10 and vue

User selected the `build-with-superpowers-x` lifecycle to carry this from idea through a delivery-ready branch. Brainstorm mode chosen: **chat** (classic `brainstorming` skill, grilling via `grilling` in chat).

## Grilling Record

Four grilling rounds, 37 settled decisions. Questions shown with the user's approved answer (all "ok" / "go ahead" = accepted recommended answer).

### Round 0 — Clarifying questions (one at a time, before grilling)

| # | Question | Answer |
|---|---|---|
| C1 | Migration shape | Strangler / parallel (new folders beside old, cutover after parity) |
| C2 | Backend architecture | Minimal Web API, keep controllers |
| C3 | Data access | Keep ADO.NET, port to Microsoft.Data.SqlClient |
| C4 | Database target | New SQL Server on the Linux host (re-run schema.sql + seed) |
| C5 | Auth strategy | JWT bearer tokens |
| C6 | Vue stack | Vue 3 + TS + Vite + Pinia |
| C7 | API DTO shape | Mirror current ViewModels |
| C8 | Scope | Strict parity first |
| C9 | Hosting | Linux host, Kestrel + static Vue |
| C10 | Component library | Element Plus |
| C11 | Verification bar | Ported smoke tests + xUnit + Vue build (+ Playwright added later) |
| C12 | Old app disposition | Keep until parity verified, then delete |

### Round 1

| # | Question | Answer |
|---|---|---|
| Q1 | .NET 10 SDK installation | Install .NET 10 SDK preview now |
| Q2 | SQL Server on Linux install method | apt repo (`mssql-server` + `mssql-tools`) |
| Q3 | JWT signing key source | Static symmetric key in user-secrets/dev config |
| Q4 | JWT storage on Vue side | httpOnly cookie set by the API, SameSite=Strict |
| Q5 | Vue project layout | `src/frontend-vue/` |
| Q6 | Repository port fidelity | Port SQL verbatim, make methods async |
| Q7 | Old verify-*.ps1 tests | Rewrite as bash + curl + jq against JSON API |
| Q8 | BCrypt dependency | Reuse BCrypt.Net-Next (latest) |
| Q9 | Seed data | Re-run existing schema.sql verbatim |
| Q10 | CORS / same-origin | Vite dev proxy `/api` → localhost:5000 |

### Round 2

| # | Question | Answer |
|---|---|---|
| Q11 | .NET 10 project type | `dotnet new webapi` + separate xUnit test project |
| Q12 | API project name/folder | `src/backend-net10/ShopApi/` |
| Q13 | appsettings environments | User-secrets for JWT key + SA password |
| Q14 | Authentication middleware | `[Authorize]` globally, role claim for future use |
| Q15 | API route conventions | REST verbs mirroring old paths |
| Q16 | Anti-forgery / CSRF | SameSite=Strict + X-Requested-With header |
| Q17 | Vue routing | History mode + SPA fallback + route guards |
| Q18 | Pinia stores | One per resource family |
| Q19 | API client / HTTP layer | Axios + 401 interceptor |
| Q20 | Build/serve prod SPA | API serves wwwroot/ with MapFallbackToFile |

### Round 3

| # | Question | Answer |
|---|---|---|
| Q21 | /api/account/me shape | Login returns user+cookie; /me for refresh hydration |
| Q22 | Pagination response envelope | PagedResult<T> { items, page, pageSize, totalPages, totalCount } |
| Q23 | Error response shape | Built-in ASP.NET Core ProblemDetails |
| Q24 | xUnit test scope | WebApplicationFactory integration tests against ShopCRMTest |
| Q25 | Test DB provisioning | Fixture (IAsyncLifetime) re-runs schema.sql per run |
| Q26 | bash smoke test split | 3 scripts mirroring old .ps1 (auth, content, crud) |
| Q27 | InteractionRepository in Details | Embed interactions in customer detail response |
| Q28 | LoggedByUsername resolution | From JWT username claim at create time |
| Q29 | Vue form validation | Element Plus rules + API ProblemDetails (both) |
| Q30 | Migration verification / cutover gate | Human declares parity after automated bars pass |

### Round 4

| # | Question | Answer |
|---|---|---|
| Q31 | .NET 10 SDK install method | install-dotnet.sh -Channel 10.0 |
| Q32 | SQL Server edition + auth on Linux | 2022 Developer, SQL auth, SA password in user-secrets |
| Q33 | TrustServerCertificate in prod | Dev-only acceptable, TODO for prod cert |
| Q34 | Connection string resilience | Bare (parity with old app) |
| Q35 | dotnet dev-certs https | Run --trust once |
| Q36 | Vite proxy target port | HTTP localhost:5000 (ASPNETCORE_URLS) |
| Q37 | Old deploy.ps1 / build-csc.ps1 | Deleted with old app at cutover |

### Frontier closure

After Round 4, the frontier was re-scanned: every branch either settled or explicitly out-of-scope (prod cert, retry config, IIS). Frontier declared empty. Shared understanding reached.

---

## 1. Architecture & layout

### Migration shape: strangler / parallel

The new .NET 10 API + Vue SPA is built in new folders beside the old app. The old `src/backend/` (.NET Framework 4.7 MVC 5) and `src/frontend/` (Razor Views) remain untouched and runnable as a live reference and fallback until parity is verified. Cutover (deletion of the old app) happens only after human sign-off.

### New repo layout (additive)

```
new-crm/
├── src/
│   ├── backend/              # OLD — .NET Framework 4.7 MVC 5, untouched until cutover
│   ├── frontend/            # OLD — Razor Views, untouched until cutover
│   ├── backend-net10/        # NEW
│   │   ├── ShopApi/          # ASP.NET Core 10 Web API (controllers), serves wwwroot/ in prod
│   │   │   ├── Controllers/  # Account, Dashboard, Customers, Interactions, Reports
│   │   │   ├── Models/       # DTOs mirroring old ViewModels + PagedResult<T>
│   │   │   ├── Repository/   # IRepositories + impls, async, Microsoft.Data.SqlClient
│   │   │   ├── Infrastructure/ # DbConnectionFactory, JwtTokenService, AuthExtensions
│   │   │   ├── sql/          # copy of schema.sql (idempotent)
│   │   │   ├── wwwroot/      # Vite build output (prod SPA)
│   │   │   ├── Program.cs
│   │   │   ├── appsettings.json / appsettings.Development.json
│   │   │   └── ShopApi.csproj
│   │   └── ShopApi.Tests/   # xUnit + WebApplicationFactory integration tests
│   └── frontend-vue/         # NEW — Vue 3 + TS + Vite + Pinia + Vue Router + Element Plus
│       ├── src/
│       │   ├── api/          # axios instances per resource
│       │   ├── stores/       # auth, customers, interactions, dashboard, reports
│       │   ├── views/        # Login, Dashboard, Customers (Index/Create/Edit/Details), Reports
│       │   ├── components/   # shared (layout, table, form widgets)
│       │   ├── router/       # history mode + guards
│       │   └── App.vue / main.ts
│       ├── vite.config.ts    # dev proxy /api → http://localhost:5000, build.outDir → ShopApi/wwwroot
│       └── package.json
├── tests/
│   ├── verify-*.ps1          # OLD smoke tests (untouched until cutover)
│   ├── smoke-auth.sh         # NEW — bash + curl + jq
│   ├── smoke-content.sh      # NEW
│   ├── smoke-crud.sh         # NEW
│   └── browser/              # NEW — Playwright specs + screenshots
│       ├── <slice>.spec.ts
│       └── screenshots/<slice>/
└── scripts/
    ├── sqlq.sh / vmexec.sh   # existing helper scripts (untouched)
    └── install-toolchain.sh  # NEW — installs .NET 10 SDK + SQL Server 2022 Dev
```

### Runtime topology

- **Dev:** Vite dev server (`localhost:5173`) proxies `/api` → `http://localhost:5000` (Kestrel, HTTP). SQL Server 2022 Developer on `localhost:1433` (SQL auth, SA password in user-secrets). Browser sends httpOnly JWT cookie automatically (same-origin via proxy).
- **Prod (local):** `dotnet publish` → single artifact; Kestrel serves API + built SPA from `wwwroot/` with `MapFallbackToFile("index.html")`. Same-origin, no CORS.

### Why this layout

`src/backend-net10/` + `src/frontend-vue/` sit beside the old `src/backend/` + `src/frontend/`, so the old app remains runnable as a live reference and fallback until parity is declared. Slice 0 creates the skeletons + toolchain; slices 1-5 fill in features.

---

## 2. Data layer

### Database

SQL Server 2022 Developer edition on the Linux host (`localhost:1433`), SQL auth. The existing `src/backend/sql/schema.sql` is reused verbatim — it's idempotent (drops + recreates `Users`, `Customers`, `Interactions` + seeds admin user, 10 customers, 5 interactions). A copy lives at `src/backend-net10/ShopApi/sql/schema.sql`. The test DB `ShopCRMTest` is created by the same script, run by the xUnit fixture.

### Connection string

`appsettings.Development.json` (non-secret parts):
```
Server=localhost,1433;Database=ShopCRM;User Id=sa;Password=<user-secrets:DbPassword>;TrustServerCertificate=True
```
Bare — no retry config (parity with old app). SA password stored in .NET user-secrets (`DbPassword`). `TrustServerCertificate=True` is dev-only; prod cert is out of scope (flagged TODO).

### Data access — ADO.NET, ported to async

- NuGet: `Microsoft.Data.SqlClient` (replaces `System.Data.SqlClient`).
- `DbConnectionFactory` → `IDbConnectionFactory` returning `SqlConnection` (injectable, testable). Static factory becomes an injectable singleton for ASP.NET Core DI.
- All repository methods become `async Task<T>`:
  - `ICustomerRepository`: `Task<List<Customer>> GetAllAsync(int page, int pageSize)`, `Task<int> GetTotalCountAsync()`, `Task<Customer> GetByIdAsync(int id)`, `Task<int> CreateAsync(Customer)`, `Task<bool> UpdateAsync(Customer)`, `Task<bool> DeleteAsync(int id)`, `Task<List<StatusCount>> GetCountByStatusAsync()`
  - `IInteractionRepository`: `Task<List<Interaction>> GetByCustomerIdAsync(int customerId)`, `Task<List<Interaction>> GetRecentAsync(int count)`, `Task<int> CreateAsync(Interaction)`
  - `IUserRepository`: `Task<User> GetByUsernameAsync(string)`, `Task<User> GetByIdAsync(int id)`
- SQL text is identical to the old repos (same `OFFSET ... FETCH NEXT`, `SCOPE_IDENTITY()`, `SYSUTCDATETIME()`). Only the C# wrappers change: `Open()` → `OpenAsync()`, `ExecuteReader()` → `ExecuteReaderAsync()`, `await` throughout.
- `MapCustomer`/`MapInteraction` static helpers ported unchanged (just `SqlDataReader` reads).

### Dependency injection

Repos registered as `Scoped` in `Program.cs` (`services.AddScoped<ICustomerRepository, CustomerRepository>()` etc.), injected into controllers via constructor injection (replaces the old `new CustomerRepository()` inline).

### Deliberate change from old behavior

`Interaction.LoggedByUsername` is set from the JWT `username` claim at create time, so the `Interactions` read query drops the Users join — it becomes a plain `SELECT * FROM dbo.Interactions WHERE CustomerId = @Id`. The `LoggedByUserId` still comes from the JWT `userId` claim.

---

## 3. API & auth

### Project

`dotnet new webapi` (controllers), .NET 10, single `ShopApi` project + `ShopApi.Tests`. `Program.cs` top-level statements register: `AddControllers()`, `AddAuthentication(JwtBearer).AddJwtBearer(...)`, `AddAuthorization()`, `AddScoped<repos>()`, `AddSingleton<IDbConnectionFactory>()`, `MapControllers()`, `MapFallbackToFile("index.html")` (prod SPA), `UseAuthentication()` + `UseAuthorization()`.

### Auth — JWT in httpOnly cookie

- On `POST /api/account/login`: verify BCrypt (`BCrypt.Net-Next`, latest) against `Users.PasswordHash`, then issue a signed JWT (HS256, symmetric key from user-secrets `Jwt:Key`) with claims `userId`, `username`, `role`. Set it as an httpOnly cookie: `shopcrm_token`, `SameSite=Strict`, `Secure` (prod) / off in dev HTTP, `HttpOnly=true`, expiry 20 min (matches old session timeout). Response body returns `{ username, role }` (no token in body).
- `AddJwtBearer` configured to read the token from the `shopcrm_token` cookie (via `Events.OnMessageReceived`) — not just the `Authorization` header.
- `POST /api/account/logout`: clears the cookie.
- `GET /api/account/me`: returns `{ userId, username, role }` from claims, or `401`. For SPA refresh hydration.
- All controllers except `AccountController`'s login are `[Authorize]`. Role claim present for future use but no role-gated routes yet (parity — old app had none).

### CSRF

`SameSite=Strict` + a custom `X-Requested-With: XMLHttpRequest` header required on all mutating endpoints (`POST`/`PUT`/`DELETE`). Enforced by a small middleware/action filter that rejects mutating requests missing the header. The SPA's Axios instance always sends it; bare form posts can't.

### Endpoints (REST verbs, mirroring old routes)

| Method | Path | Returns |
|---|---|---|
| POST | `/api/account/login` | `{ username, role }` + sets cookie |
| POST | `/api/account/logout` | 204 |
| GET | `/api/account/me` | `{ userId, username, role }` |
| GET | `/api/dashboard` | `DashboardViewModel` (totalCustomers, statusCounts, recentInteractions, username) |
| GET | `/api/customers?page=N` | `PagedResult<CustomerDto>` |
| POST | `/api/customers` | `201` + `CustomerDto` |
| GET | `/api/customers/{id}` | `{ customer: CustomerDto, interactions: InteractionDto[] }` (embeds interactions) |
| PUT | `/api/customers/{id}` | `CustomerDto` |
| DELETE | `/api/customers/{id}` | 204 |
| GET | `/api/customers/{id}/interactions` | `InteractionDto[]` |
| POST | `/api/interactions` | `201` + `InteractionDto` (sets `LoggedByUserId`/`LoggedByUsername` from JWT) |
| GET | `/api/reports` | `ReportViewModel` (statusCounts, totalCustomers) |

### DTOs

Mirror old ViewModels verbatim: `LoginRequest`, `CustomerDto` (with validation attributes: `Required`, `StringLength(100)`, `EmailAddress`), `InteractionDto`, `DashboardDto`, `ReportDto`, `StatusCountDto`, plus `PagedResult<T> { items, page, pageSize, totalPages, totalCount }`.

### Errors

Built-in ASP.NET Core ProblemDetails — `ValidationProblemDetails` (400) auto-returns model-state errors as `{ errors: { Name: ["Name is required"] } }`; `ProblemDetails` for 404/500. Controllers use `NotFound()` / `BadRequest(ModelState)`.

### Config

`appsettings.json` (base, no secrets), `appsettings.Development.json` (connection string template, non-secret), user-secrets for `DbPassword` + `Jwt:Key`. Dev runs on `ASPNETCORE_URLS=http://localhost:5000`. `dotnet dev-certs https --trust` run once (kept for prod parity, but dev uses HTTP to simplify the Vite proxy).

---

## 4. Frontend (Vue SPA)

### Stack

Vue 3 (Composition API, `<script setup>`) + TypeScript + Vite + Pinia + Vue Router (history mode) + Element Plus + Axios.

### Project

`src/frontend-vue/`, scaffolded via `npm create vue@latest` (TS, Router, Pinia enabled). `vite.config.ts`:
- `server.proxy['/api']` → `http://localhost:5000` (dev proxy, same-origin in browser)
- `build.outDir` → `../backend-net10/ShopApi/wwwroot` (so `npm run build` feeds the API's static serve)
- `server.port` → 5173

### Routing

`src/router/index.ts`, history mode, routes mirror the old app:

| Path | View | Guard |
|---|---|---|
| `/login` | LoginView | redirect to `/dashboard` if authed |
| `/dashboard` | DashboardView | requires auth |
| `/customers` | CustomersIndexView | requires auth |
| `/customers/create` | CustomerCreateView | requires auth |
| `/customers/:id` | CustomerDetailsView | requires auth |
| `/customers/:id/edit` | CustomerEditView | requires auth |
| `/reports` | ReportsView | requires auth |
| `/:pathMatch(.*)*` | NotFoundView | — |

Route guard: `beforeEach` checks `authStore.isAuthenticated`; if not, redirect to `/login?redirect=<to.fullPath>`. `authStore` is hydrated on boot (in `main.ts`) by calling `GET /api/account/me` — if 200, set `user/role`; if 401, clear. `isAuthenticated` is reactive off the store state.

### Pinia stores (one per resource family)

- `authStore`: `user`, `role`, `isAuthenticated`; `login(creds)`, `logout()`, `fetchMe()`
- `customersStore`: `list`, `pagination` (page/pageSize/totalPages/totalCount), `current` (customer + interactions), `fetchAll(page)`, `fetchOne(id)`, `create(dto)`, `update(id, dto)`, `remove(id)`
- `interactionsStore`: `byCustomer`, `fetchByCustomer(id)`, `create(dto)`
- `dashboardStore`: `data`, `fetch()`
- `reportsStore`: `data`, `fetch()`

### API layer

`src/api/`: one Axios instance per resource (`auth.ts`, `customers.ts`, `interactions.ts`, `dashboard.ts`, `reports.ts`). A shared `axios.create({ baseURL: '/api', withCredentials: true, headers: { 'X-Requested-With': 'XMLHttpRequest' } })` in `src/api/client.ts`. Response interceptor: on `401`, clear `authStore` + redirect to `/login`. No manual token attachment (httpOnly cookie is auto-sent, same-origin via proxy).

### Views (parity mapping)

- **LoginView** — Element Plus `el-form` + `el-input`, fields `username`/`password`, validation rules mirroring old `LoginViewModel` (`Required`). On submit → `authStore.login()` → redirect to `/dashboard` (or `?redirect=`).
- **DashboardView** — stat cards (`el-statistic` or `el-card`) for totalCustomers + statusCounts; recent interactions list (`el-table` or `el-timeline`). `onMounted` → `dashboardStore.fetch()`.
- **CustomersIndexView** — `el-table` with columns (Name, Email, Phone, Company, Status), `el-pagination` bound to `customersStore.pagination`, "Create" button → `/customers/create`, row click → `/customers/:id`. Page size 10 (parity).
- **CustomerCreateView** — `el-form` with fields + Element Plus rules mirroring `CustomerViewModel` annotations (`Required` Name/Status, `StringLength(100)` Name/Email/Company, `StringLength(30)` Phone, `EmailAddress` Email). On submit → `customersStore.create()` → redirect to `/customers`. Server `ValidationProblemDetails` errors mapped back to form fields.
- **CustomerEditView** — same form, pre-filled from `customersStore.fetchOne(id)`, `update()` on submit.
- **CustomerDetailsView** — customer fields + embedded interactions list (`el-table`); "Log interaction" button opens an `el-dialog` with `el-form` (Type select, Note textarea) → `interactionsStore.create()`.
- **ReportsView** — status counts table + bars (`el-progress` or simple CSS bar), mirrors old Reports view. `onMounted` → `reportsStore.fetch()`.

### Layout

`App.vue` has a top nav (`el-menu`: Dashboard, Customers, Reports, Logout) shown when authed; `LoginView` renders without nav. Matches old `_Layout.cshtml` vs `_LoginLayout.cshtml` split.

### Validation

Element Plus form rules for instant client-side feedback (mirroring old `DataAnnotations`); API `ProblemDetails.errors` mapped to form fields after submit for server-side authority. Client rules are the first line, API is the source of truth.

### Build/typecheck bar

`npm run build` (Vite + `vue-tsc`) must pass with zero TS errors. `npm run type-check` runs in the verification step.

---

## 5. Testing & verification

### Four verification layers

#### 1. xUnit integration tests (`ShopApi.Tests`)

- `WebApplicationFactory<Program>` boots the full API in-memory against a real test DB (`ShopCRMTest`).
- A shared `[CollectionDefinition]` fixture (`IAsyncLifetime`) runs `schema.sql` against `ShopCRMTest` once at collection start (idempotent — drops + recreates). Connection string points to `ShopCRMTest` via test `appsettings` or env var.
- Test cases (one per feature, end-to-end through the HTTP pipeline):
  - `LoginTests`: valid creds → 200 + cookie set + `{ username, role }`; invalid creds → 401.
  - `DashboardTests`: authed GET → 200 with `totalCustomers`, `statusCounts`, `recentInteractions`.
  - `CustomersTests`: list (page 1) → paged result; create → 201; get by id → 200 with interactions; update → 200; delete → 204; invalid create → 400 ValidationProblemDetails.
  - `InteractionsTests`: create → 201 with `LoggedByUsername` from JWT; list by customer → 200.
  - `ReportsTests`: authed GET → 200 with statusCounts + totalCustomers.
  - `AuthTests`: unauthed GET `/api/dashboard` → 401; `/api/account/me` → 401 without cookie, 200 with.
- Bar: all tests green.

#### 2. Bash smoke tests (`tests/smoke-*.sh`)

Mirror the old PowerShell tests, retargeted to the JSON API. Each uses `curl` + `jq`, runs against the live dev server (`http://localhost:5000`):
- `smoke-auth.sh`: POST login with admin/Admin@123 → expect 200 + `Set-Cookie: shopcrm_token`; GET `/api/account/me` with cookie → expect 200 + `username=admin`; bad password → 401.
- `smoke-content.sh`: authed GET `/api/dashboard` → 200 + `totalCustomers` >= 10; GET `/api/reports` → 200 + statusCounts array.
- `smoke-crud.sh`: create customer → 201 + parse `id`; GET `/api/customers/{id}` → 200 + interactions array; POST interaction → 201 + `LoggedByUsername=admin`; PUT customer → 200; DELETE → 204; GET deleted → 404.
- Each script `exit 0` on all assertions passing, non-zero on failure. Runnable standalone (`bash tests/smoke-crud.sh`).

#### 3. Vue build + typecheck

- `npm run type-check` (vue-tsc) → zero errors.
- `npm run build` → zero errors, outputs to `ShopApi/wwwroot/`.

#### 4. Browser verification (playwright-cli) — MANDATORY for every UI slice

Per the `using-superpowers-x` lifecycle, every UI-affecting slice (Login, Dashboard, Customers CRUD, Interactions, Reports) must be verified through the `browser-verification` skill before the slice is considered done. "Tests pass" never proves a page renders.

For each slice, a Playwright script drives a real browser (Chromium) against the running Vite dev server (`http://localhost:5173`) + API (`http://localhost:5000`), and captures:
- A screenshot of each new view in its rendered state (login form, dashboard cards, customers table, create/edit form, details + interactions, reports).
- Console logs (no `error` level entries allowed).
- A click-through of the slice's core flow (e.g. login → dashboard → customers → create → save → row appears; details → log interaction → appears in list).

Stored under `tests/browser/<slice>.spec.ts` (Playwright test format) + `tests/browser/screenshots/<slice>/`. Re-run on every change to that slice.

Bar: screenshots show the expected UI, console is clean, click-through succeeds. Failure blocks the slice from being marked complete.

### Parity checklist (human sign-off gate)

After all four automated layers pass (xUnit + bash smoke + Vue build/typecheck + Playwright browser verification), a manual click-through of all 5 feature areas (login, dashboard, customer CRUD, interactions, reports) must match the old app's behavior. The spec lists this checklist explicitly.

**Cutover** (delete `src/backend/` + `src/frontend/` + old `verify-*.ps1` + `deploy.ps1`/`build-csc.ps1`) only happens after the user says "parity confirmed."

### Verification cadence per slice

Each feature slice lands its xUnit tests + the relevant smoke script + its Playwright browser spec in the same slice. Slice 0 lands the toolchain + skeleton + a `/health` smoke check (no UI yet, so no Playwright).

---

## 6. Slice breakdown & sequence

6 slices, each independently verifiable. Slice 0 is infra; slices 1-5 are vertical features. Each feature slice lands its xUnit tests + bash smoke script + Playwright browser spec. Strictly sequential (0 → 1 → 2 → 3 → 4 → 5); no parallelism in v1 per the build-with-superpowers-x policy.

### Slice 0 — Toolchain & skeletons (infra)

- Install .NET 10 SDK (via `install-dotnet.sh -Channel 10.0`), SQL Server 2022 Developer (apt), `dotnet dev-certs https --trust`. Node 22 already present.
- `scripts/install-toolchain.sh` automates the SDK + SQL Server install + SA password prompt → user-secrets.
- Run `schema.sql` against `localhost:1433` → `ShopCRM` DB with seed data.
- `dotnet new webapi` → `src/backend-net10/ShopApi/` + `dotnet new xunit` → `src/backend-net10/ShopApi.Tests/`, solution file.
- `npm create vue@latest` → `src/frontend-vue/` (TS, Router, Pinia, Element Plus added).
- `Program.cs`: DI wiring, `MapGet("/health")`, `MapFallbackToFile`. `vite.config.ts`: proxy + outDir.
- Verification: `dotnet build` + `dotnet run` → `GET /health` 200; `npm run dev` → Vite serves; `tests/smoke-health.sh` (curl `/health`). No UI → no Playwright yet.

### Slice 1 — Login (auth)

- Backend: `AccountController` (login/logout/me), `JwtTokenService`, `IDbConnectionFactory` + `UserRepository` (async, BCrypt verify), JWT bearer reading from cookie, `X-Requested-With` middleware, `[Authorize]` wiring.
- Frontend: `LoginView`, `authStore` (login/logout/fetchMe), `auth.ts` Axios, route guard, `App.vue` nav with Logout.
- Tests: `LoginTests` + `AuthTests` xUnit; `smoke-auth.sh`; Playwright `login.spec.ts` (screenshot login form + successful login redirect + bad password error).

### Slice 2 — Dashboard

- Backend: `DashboardController` GET `/api/dashboard`, `CustomerRepository.GetCountByStatusAsync` + `GetTotalCountAsync`, `InteractionRepository.GetRecentAsync`, `DashboardDto`.
- Frontend: `DashboardView`, `dashboardStore`, `dashboard.ts`, stat cards + recent interactions list.
- Tests: `DashboardTests` xUnit; `smoke-content.sh` (dashboard part); Playwright `dashboard.spec.ts` (screenshot rendered dashboard, verify totalCustomers + recent interactions visible).

### Slice 3 — Customers CRUD

- Backend: `CustomersController` (list/create/get/update/delete), `CustomerRepository` full async port, `CustomerDto` + `PagedResult<T>`, validation → ProblemDetails.
- Frontend: `CustomersIndexView` (table + pagination), `CustomerCreateView`, `CustomerEditView`, `customersStore`, `customers.ts`, Element Plus form rules + API error mapping.
- Tests: `CustomersTests` xUnit; `smoke-crud.sh` (customer part); Playwright `customers.spec.ts` (screenshot table, create → row appears, edit → changes persist, delete → row gone).

### Slice 4 — Interactions

- Backend: `InteractionsController` (create, list by customer), `InteractionRepository` async, `InteractionDto`, `LoggedByUsername` from JWT claim, embedded interactions in customer detail response.
- Frontend: `CustomerDetailsView` (customer + interactions list + log-interaction dialog), `interactionsStore`, `interactions.ts`.
- Tests: `InteractionsTests` xUnit; `smoke-crud.sh` (interaction part extended); Playwright `interactions.spec.ts` (screenshot details page, log interaction → appears in list).

### Slice 5 — Reports + cutover

- Backend: `ReportsController` GET `/api/reports`, `ReportDto`.
- Frontend: `ReportsView` (status counts table + bars), `reportsStore`, `reports.ts`.
- Tests: `ReportsTests` xUnit; `smoke-content.sh` (reports part); Playwright `reports.spec.ts` (screenshot reports page, verify bars render).
- **Parity gate:** all 4 automated layers green across all slices → manual click-through of all 5 features vs old app → user confirms parity → delete `src/backend/`, `src/frontend/`, old `verify-*.ps1`, `deploy.ps1`, `build-csc.ps1`. Commit cutover.

### Slice dependency

0 → 1 → 2 → 3 → 4 → 5 (strictly sequential; each builds on the prior slice's auth/repo/API patterns). No parallelism in v1.

---

## Appendix — Settled decisions (37 from grilling)

| # | Decision | Choice |
|---|---|---|
| Migration shape | Strangler / parallel | New folders beside old; cutover after parity |
| Backend architecture | Minimal Web API, keep controllers | Closest to current controller shape |
| Data access | ADO.NET → Microsoft.Data.SqlClient | Smallest change, SQL verbatim |
| Database target | New SQL Server on Linux host | Re-run schema.sql + seed |
| Auth strategy | JWT bearer tokens | Stateless, httpOnly cookie |
| Vue stack | Vue 3 + TS + Vite + Pinia | Idiomatic modern Vue |
| API DTO shape | Mirror current ViewModels | 1:1 port |
| Scope | Strict parity first | No new features until parity |
| Hosting | Linux host, Kestrel + static Vue | No IIS/VM for new stack |
| Component library | Element Plus | Dense admin components |
| Verification bar | Ported smoke + xUnit + Vue build | + Playwright (added) |
| Old app disposition | Keep until parity, then delete | Fallback + live reference |
| Q1 | .NET 10 SDK | Preview via install-dotnet.sh |
| Q2 | SQL Server install | apt repo |
| Q3 | JWT signing key | Static symmetric in user-secrets |
| Q4 | JWT storage | httpOnly cookie, SameSite=Strict |
| Q5 | Vue project layout | src/frontend-vue/ |
| Q6 | Repo port | Verbatim SQL, async methods |
| Q7 | Smoke tests | bash + curl + jq |
| Q8 | BCrypt | Reuse BCrypt.Net-Next (latest) |
| Q9 | Seed data | Re-run existing schema.sql |
| Q10 | CORS | Vite dev proxy, same-origin |
| Q11 | .NET project type | webapi + xUnit test project |
| Q12 | API folder | src/backend-net10/ShopApi/ |
| Q13 | Config | user-secrets for JWT key + SA password |
| Q14 | Auth middleware | [Authorize] globally, role claim for future |
| Q15 | API routes | REST verbs mirroring old paths |
| Q16 | CSRF | SameSite=Strict + X-Requested-With header |
| Q17 | Vue routing | History mode + SPA fallback |
| Q18 | Pinia stores | One per resource family |
| Q19 | HTTP layer | Axios + 401 interceptor |
| Q20 | Prod SPA serve | API serves wwwroot/ with fallback |
| Q21 | /api/account/me | For refresh hydration; login returns user+cookie |
| Q22 | Pagination | PagedResult<T> envelope |
| Q23 | Errors | Built-in ProblemDetails |
| Q24 | xUnit scope | WebApplicationFactory integration tests |
| Q25 | Test DB | Fixture re-runs schema.sql |
| Q26 | Smoke split | 3 scripts mirroring old .ps1 |
| Q27 | Customer detail | Embeds interactions |
| Q28 | LoggedByUsername | From JWT claim at create time |
| Q29 | Form validation | Element Plus rules + API ProblemDetails |
| Q30 | Parity gate | Human declares after automated bars pass |
| Q31 | SDK install method | install-dotnet.sh -Channel 10.0 |
| Q32 | SQL Server edition | 2022 Developer, SQL auth |
| Q33 | TrustServerCertificate | Dev-only, TODO for prod |
| Q34 | Connection resilience | Bare (parity) |
| Q35 | dev-certs https | Run --trust once |
| Q36 | Dev port | HTTP localhost:5000 |
| Q37 | Old scripts | Deleted with old app at cutover |
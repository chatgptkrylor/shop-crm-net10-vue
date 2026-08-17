# Specification — Tiny CRM .NET 10 + Vue Migration

**Feature ID:** net10-vue-migration
**Date:** 2026-08-13
**Status:** Canonical specification (implementation-neutral)
**Design source:** `docs/superpowers/specs/2026-08-13-net10-vue-migration-design.md`
**Design source SHA-256:** `619c3614af2ae1ba9c0a69fb6ad759501caf1e220b079c755f05992353c1e84a`

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

## Purpose

Migrate the Tiny CRM application from .NET Framework 4.7 / ASP.NET MVC 5 / Razor to .NET 10 (preview) Web API + Vue 3 SPA, running on the Linux host. Deliver strict feature parity with the existing application. Preserve the existing app untouched as a live reference and fallback until parity is verified, then remove it.

## Scope

**In scope:**
- A new .NET 10 Web API (controllers) backend exposing JSON endpoints for the five existing feature areas: login, dashboard, customer CRUD, interactions, reports.
- A new Vue 3 + TypeScript SPA frontend implementing the same five feature areas with the same behavior.
- A new SQL Server 2022 Developer instance on the Linux host, populated from the existing `schema.sql` (unchanged).
- JWT-based authentication issued by the API and consumed by the SPA via an httpOnly cookie.
- ADO.NET data access (async) against the new SQL Server, using `Microsoft.Data.SqlClient`.
- Automated verification: xUnit integration tests, bash smoke tests, Vue build + typecheck, and Playwright browser verification for every UI slice.
- A strangler/parallel migration: the new stack lives in new folders beside the old; the old stack is deleted only after parity is confirmed.

**Out of scope (explicitly excluded):**
- New product features beyond parity (search/filter, role-based UI, etc.) — deferred until after parity.
- Production HTTPS certificate configuration (dev uses `TrustServerCertificate=True`; prod cert is a flagged TODO).
- Connection-string retry/resilience policies (bare, for parity).
- IIS / Windows VM deployment of the new stack (new stack runs on the Linux host).
- Containerization (Docker Compose) — deferred.
- Role-based authorization gating (role claim is carried for future use; no role-gated routes exist in the parity scope).
- External identity provider / OpenID Connect.
- EF Core / Dapper migration (ADO.NET is retained).

## Requirements

### Functional requirements

The migrated application SHALL provide the same user-visible behavior as the existing Tiny CRM across five feature areas:

1. **Login.** A user SHALL be able to log in with a username and password. The system SHALL verify the password against the stored BCrypt hash. On success, the system SHALL issue a signed JWT in an httpOnly cookie and return the user's username and role. On failure, the system SHALL reject the login. A logged-in user SHALL be able to log out, which clears the cookie. The SPA SHALL hydrate the authenticated state on page refresh via a `/api/account/me` endpoint.

2. **Dashboard.** An authenticated user SHALL see a dashboard showing the total number of customers, a breakdown of customers by status (Lead/Contact/Customer), and a list of recent interactions. The dashboard SHALL be accessible only to authenticated users.

3. **Customer CRUD.** An authenticated user SHALL be able to list customers with pagination (10 per page), create a new customer, view a customer's details (including its interactions), edit a customer, and delete a customer. The customer fields are: Name (required, max 100), Email (max 100, valid email), Phone (max 30), Company (max 100), Status (required, one of Lead/Contact/Customer). Validation errors SHALL be surfaced to the user.

4. **Interactions.** An authenticated user SHALL be able to view the interactions logged against a customer and log a new interaction against a customer. An interaction has a Type (Call/Email/Meeting/Note, required) and a Note (required, non-empty). The logged-in user's username SHALL be recorded as `LoggedByUsername` and their user id as `LoggedByUserId`.

5. **Reports.** An authenticated user SHALL see a report of customers grouped by status with counts and the total customer count.

### Non-functional requirements

- **Platform:** The new stack SHALL run on the Linux host. The API SHALL be served by Kestrel on `http://localhost:5000` in dev. The SPA SHALL be served by the Vite dev server on `http://localhost:5173` in dev, proxying `/api` to the API. In a local prod build, the API SHALL serve the built SPA from `wwwroot/` with a SPA fallback to `index.html`.
- **Performance:** The API SHALL use async data access throughout. No sync-over-async.
- **Parity:** The migrated application SHALL match the existing application's behavior across all five feature areas. Parity is the acceptance bar; no new features are added before parity.
- **Maintainability:** The codebase SHALL follow idiomatic .NET 10 (top-level `Program.cs`, DI, async/await, ProblemDetails) and idiomatic Vue 3 (Composition API, `<script setup>`, Pinia, TypeScript).
- **Tooling:** The .NET 10 SDK (preview) and SQL Server 2022 Developer SHALL be installed on the Linux host. Node 22 is already present.

## Interfaces

### HTTP API (the product contract)

All endpoints are under `/api`. All mutating endpoints (`POST`/`PUT`/`DELETE`) SHALL require an `X-Requested-With: XMLHttpRequest` header. All endpoints except `POST /api/account/login` SHALL require authentication (valid JWT in the `shopcrm_token` httpOnly cookie).

| Method | Path | Request body | Success response |
|---|---|---|---|
| POST | `/api/account/login` | `{ username: string, password: string }` | `200 { username: string, role: string }` + `Set-Cookie: shopcrm_token` |
| POST | `/api/account/logout` | — | `204` + clears cookie |
| GET | `/api/account/me` | — | `200 { userId: int, username: string, role: string }` or `401` |
| GET | `/api/dashboard` | — | `200 { totalCustomers: int, statusCounts: [{ status: string, count: int }], recentInteractions: [Interaction], username: string }` |
| GET | `/api/customers?page=N` | — | `200 { items: [Customer], page: int, pageSize: int, totalPages: int, totalCount: int }` |
| POST | `/api/customers` | `CustomerDto` | `201 CustomerDto` |
| GET | `/api/customers/{id}` | — | `200 { customer: CustomerDto, interactions: [Interaction] }` or `404` |
| PUT | `/api/customers/{id}` | `CustomerDto` | `200 CustomerDto` or `404` |
| DELETE | `/api/customers/{id}` | — | `204` or `404` |
| GET | `/api/customers/{id}/interactions` | — | `200 [Interaction]` |
| POST | `/api/interactions` | `{ customerId: int, type: string, note: string }` | `201 Interaction` |
| GET | `/api/reports` | — | `200 { statusCounts: [{ status: string, count: int }], totalCustomers: int }` |

**Data shapes:**
- `Customer`: `{ id: int, name: string, email: string|null, phone: string|null, company: string|null, status: string, createdAt: datetime, updatedAt: datetime|null, createdByUserId: int }`
- `Interaction`: `{ id: int, customerId: int, type: string, note: string, loggedAt: datetime, loggedByUserId: int, loggedByUsername: string }`

**Pagination:** default page size 10. `page` is 1-indexed. `totalPages = ceil(totalCount / pageSize)`.

### Database schema

Unchanged from the existing `src/backend/sql/schema.sql`: tables `dbo.Users`, `dbo.Customers`, `dbo.Interactions` with the existing columns, constraints, and seed data (admin user `admin` / `Admin@123`, 10 customers, 5 interactions). The schema is idempotent (drops + recreates).

### Auth cookie

- Name: `shopcrm_token`
- Attributes: `HttpOnly`, `SameSite=Strict`, `Secure` (prod HTTPS) / off in dev HTTP, expiry 20 minutes.
- Value: a signed JWT (HS256) with claims `userId`, `username`, `role`.

## Failure behavior

- **Unauthenticated request to a protected endpoint:** `401 Unauthorized` (ProblemDetails).
- **Invalid login credentials:** `401 Unauthorized` (ProblemDetails with a generic "Invalid username or password" message — no user enumeration).
- **Validation failure on a mutating endpoint:** `400 Bad Request` with `ValidationProblemDetails` whose `errors` object maps field names to arrays of error messages.
- **Resource not found (customer by id):** `404 Not Found` (ProblemDetails).
- **Missing `X-Requested-With` header on a mutating endpoint:** `400 Bad Request`.
- **Server error:** `500` (ProblemDetails). Stack traces hidden in non-dev.
- **SPA route not found:** the SPA fallback serves `index.html`; Vue Router shows a NotFound view.
- **API unreachable during SPA use:** the Axios 401 interceptor clears the auth store and redirects to `/login`. Network errors surface a user-facing error state.

## Security constraints

- Passwords SHALL be verified using BCrypt (`BCrypt.Net-Next`). The existing seed hash SHALL remain valid (no re-hashing).
- The JWT signing key SHALL be stored in .NET user-secrets (dev) and not committed to the repository.
- The SQL Server SA password SHALL be stored in .NET user-secrets (dev) and not committed.
- The auth cookie SHALL be `HttpOnly` (not readable by JavaScript) and `SameSite=Strict`.
- All mutating API endpoints SHALL require the `X-Requested-With: XMLHttpRequest` header in addition to authentication, to prevent CSRF.
- The login endpoint SHALL return a generic error on bad credentials (no disclosure of which of username/password was wrong).
- `TrustServerCertificate=True` is permitted in dev only; prod SHALL use a real certificate (out of scope, flagged TODO).
- No secrets, keys, or passwords SHALL be committed to the repository.

## Acceptance criteria

The migration is complete when ALL of the following are true:

1. **xUnit integration tests:** the `ShopApi.Tests` suite passes, covering login (success + failure), `/api/account/me` (authed + unauthed), dashboard, customer CRUD (list/create/get/update/delete + validation failure), interactions (create + list, with `LoggedByUsername` from JWT), and reports.
2. **Bash smoke tests:** `tests/smoke-auth.sh`, `tests/smoke-content.sh`, and `tests/smoke-crud.sh` all exit 0 against the running dev stack.
3. **Vue build + typecheck:** `npm run type-check` and `npm run build` both succeed with zero errors, and the build output lands in `src/backend-net10/ShopApi/wwwroot/`.
4. **Playwright browser verification:** a Playwright spec exists and passes for each UI slice (login, dashboard, customers, interactions, reports), capturing screenshots of each view, asserting a clean browser console, and completing a click-through of the slice's core flow.
5. **Feature parity (manual human sign-off):** a manual click-through of all five feature areas (login, dashboard, customer CRUD, interactions, reports) matches the existing application's behavior. The user explicitly confirms parity.
6. **Cutover:** only after criteria 1-5 are met, the old `src/backend/`, `src/frontend/`, `tests/verify-*.ps1`, `src/backend/deploy.ps1`, and `src/backend/build-csc.ps1` are deleted in a single commit.

## Exclusions (restated for clarity)

- No new product features before parity.
- No production HTTPS cert, no retry policies, no IIS/VM, no Docker, no external IdP, no EF Core/Dapper, no role-gated routes.

---

## Specification SHA-256

To be computed after the user approves the spec content; the hash below is filled by the `to-spec` skill's final step.

See `docs/superpowers/specs/net10-vue-migration.manifest.json` for the specification SHA-256 (recorded in a sidecar to avoid self-reference).
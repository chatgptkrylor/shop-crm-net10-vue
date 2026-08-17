# Tiny CRM — Restore original UI on the new stack

**Goal:** The .NET 10 + Vue 3 app must look and behave like the original Bootstrap 4 Tiny CRM. Stack stays. Chrome, copy, layout, colors, and controls match the Razor screenshots 1:1.

**Source of truth:** `src/frontend/Views/**/*.cshtml` and the TINYCRM verification screenshots.

**Current problem:** `src/frontend-vue` was built with Element Plus. That is a different design system (white header, el-table, el-card, different buttons, missing Reports / interaction form / footer).

---

## Approach

Keep Vue 3 + Pinia + Vue Router + Axios + the .NET 10 API.

Drop Element Plus.

Load the same CSS the original used:

`https://cdn.jsdelivr.net/npm/bootstrap@4.6.2/dist/css/bootstrap.min.css`

Rebuild every page with the original Bootstrap class names and copy. No redesign.

---

## Page-by-page parity

### Shell (every authenticated page)

- Dark navbar: `navbar navbar-expand-lg navbar-dark bg-dark`
- Brand: `Tiny CRM` → `/dashboard`
- Left links: Dashboard, Customers, Reports
- Right: `Hello, {username}` + Logout
- Body: `container mt-4`
- Footer: `Tiny CRM — .NET Framework 4.7 / ASP.NET MVC 5 / IIS on WIN-IIS-DEV`

### Login (no navbar)

- Centered `card shadow` on white full-height page
- Title `Tiny CRM`, subtitle `Sign in to your account`
- Labeled Username / Password fields (`form-control`)
- Full-width blue `Sign In` button
- `Demo: admin / Admin@123`
- Validation: `Username is required` / `Password is required` in `text-danger small`

### Dashboard

- `h2` Dashboard
- `Welcome back, {username}!`
- Left: blue `card text-white bg-primary` with `display-4` total
- Right: `Customers by Status` table (`table table-sm`)
- Bottom-left: Recent Interactions as `list-group` (type badge + note + `user · MMM dd`)
- Bottom-right: Quick Access — green New Customer, teal View Reports, outline-red Logout

### Customers list

- `h2` Customers + green `New Customer` (not “Create Customer”)
- `table table-striped table-hover` with `thead-dark`
- Columns: Name, Email, Phone, Company, Status, Actions
- Status badges: Lead=warning, Contact=info, Customer=success
- Actions: teal Details + yellow Edit (row is not clickable)
- Centered numbered pagination when `totalPages > 1`

### New / Edit Customer

- Headings: `New Customer` / `Edit Customer`
- Stacked `form-group` + `form-control` fields: Name, Email, Phone, Company, Status select
- New: green **Create** + gray **Cancel**
- Edit: yellow **Save** + gray **Cancel**
- Name required error: `text-danger`

### Customer details

- `h2` = customer name, yellow Edit
- Customer Info card (`dl.row`): Email, Phone, Company, Status badge, Created (`MMM dd, yyyy`)
- Interaction History list (type badge + note + `user · MMM dd, yyyy HH:mm`)
- Empty: `No interactions logged yet.`
- Inline log form: Type select (Call/Email/Meeting/Note), Note text, blue **Log**
- Gray **Back to List**

### Reports

- `h2` Customer Report
- `Total customers: N`
- Card “Customers by Status” with Status / Count / Distribution progress bars (`height: 20px`, integer %)

---

## Backend gaps the UI needs

| Gap | Fix |
|-----|-----|
| No `/api/reports` | Add `ReportsController` |
| No create/list interactions | Add `InteractionsController` (`POST /api/interactions`, `GET /api/customers/{id}/interactions`) |
| `CustomerDto` missing `CreatedAt` | Add it; return from GetById |
| Interaction username empty | SELECT via `JOIN dbo.Users` (schema has no `LoggedByUsername` column) |

---

## Out of scope

- Changing API auth, JWT, or SQL schema
- Redesigning the UI
- Deleting the old Razor app
- Replacing Bootstrap with a newer major version

---

## Verification

1. `dotnet test` in `src/backend-net10`
2. `npm run build` + `vue-tsc` in `src/frontend-vue`
3. Side-by-side browser check of login, dashboard, customers, details, create, edit, reports against the original screenshots

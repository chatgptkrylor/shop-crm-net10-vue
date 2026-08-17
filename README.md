# Tiny CRM — .NET 10 + Vue

A small CRM for login, a dashboard, customer records, interaction notes, and a status report.

This repo is the **upgraded** Tiny CRM: Vue 3 on the front, .NET 10 Web API on the back.

The original .NET Framework 4.7 / Razor / IIS app is in [tiny-crm](https://github.com/chatgptkrylor/tiny-crm). Both share the `ShopCRM` database on WIN-IIS-DEV.

## Stack

| Layer | Tech |
|---|---|
| UI | Vue 3, TypeScript, Vite, Bootstrap 4 |
| API | .NET 10, Kestrel |
| Auth | Server-side sessions in SQL (`dbo.Sessions`), 20-minute timeout |
| Data | SQL Server `ShopCRM` on the Windows host (shared with the original Tiny CRM) |

## Run it

```bash
./start-app.sh
```

Then open:

- UI: http://localhost:5173/login
- API: http://127.0.0.1:5000/health
- Tailscale: `http://<this-host>.tail2e3aa.ts.net:5173`

Demo login: `admin` / `Admin@123`

`./start-app.sh --status` prints what is already up.

## Layout

```
src/frontend-vue/     Vue 3 SPA
src/backend-net10/    .NET 10 API
scripts/              start-app.sh and helpers
tests/                API smoke + e2e
```

## Tests

```bash
dotnet test src/backend-net10
bash tests/e2e-full.sh
```

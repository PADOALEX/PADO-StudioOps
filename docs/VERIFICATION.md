# Verification evidence and release gates

## Executed in authoring environment

- [x] Both uploaded archives opened and inventoried; original ten-table schema and Task Board JavaScript inspected.
- [x] Historical SQL material copied unchanged to `legacy-labs/` and excluded from runtime and Docker build context.
- [x] `node --check src/PADO.StudioOps.Api/wwwroot/js/board.js` completed without syntax errors.
- [x] `.env` excluded by `.gitignore` / `.dockerignore`; no real credentials introduced into application code.
- [x] .NET 10 / Npgsql 10 / ASP.NET Core package versions cross-checked against NuGet.

## Not executed in authoring environment

- [ ] `dotnet restore/build/test` — **.NET SDK not installed** in the available sandbox.
- [ ] EF migration applied to PostgreSQL — **PostgreSQL runtime not installed**.
- [ ] `docker compose up --build` — **Docker not installed**.
- [ ] Live browser E2E, production security scan and screenshots — require a running app.
- [x] Initial GitHub Actions run 39132792422: restore/build and 10 unit tests passed; integration test exposed missing startup configuration.
- [x] EF Core migration and immutable model snapshot regenerated with the official `dotnet ef` tool on GitHub Actions. The baseline includes eleven tables, check constraints, FKs and indexes.
- [x] GitHub Actions run [39134647312](https://github.com/PADOALEX/PADO-StudioOps/actions/runs/39134647312) on 2026-10-18 completed **successfully**:
  - .NET Release build: **0 errors, 0 warnings**.
  - Unit tests: **10 passed / 0 failed**.
  - Integration tests with a PostgreSQL 16 service: **1 passed / 0 failed**.
  - Python structural checks and JavaScript syntax validation: passed.
  - `dotnet ef migrations has-pending-model-changes`: passed; model and snapshot are synchronized.
- [ ] Browser UI acceptance, a full `docker compose up --build` smoke test and deployment-level security testing have not yet been performed. Do not claim these checks are completed.
- [ ] Green integration tests and live browser acceptance remain release gates.

Do not treat this as a green CI run. Resolve any build/migration/runtime issues revealed by a real SDK + PostgreSQL execution **before** calling the application release-ready or claiming successful deployment in a CV.

## Release verification on Windows

```powershell
dotnet --info
dotnet restore PADO.StudioOps.slnx
dotnet build PADO.StudioOps.slnx -c Release
node --check src/PADO.StudioOps.Api/wwwroot/js/board.js
# Configure STUDIOOPS_TEST_DB for a disposable database before integration testing.
dotnet test tests/PADO.StudioOps.UnitTests -c Release
dotnet test tests/PADO.StudioOps.IntegrationTests -c Release
Copy-Item .env.example .env
# Edit both passwords in .env
docker compose up --build -d
docker compose ps
docker compose logs --tail 200 api
```

Manual acceptance: login with demo admin, view Blind Protocol, move a task from To Do to Done, refresh the page, verify updated progress, create milestone/ticket/build, test access control with a separate developer account, and capture authentic screenshots.

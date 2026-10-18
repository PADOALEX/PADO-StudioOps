# Publishing this standalone repo

Suggested GitHub repository: `PADOALEX/PADO-StudioOps`

**Description:** Game development studio management platform built with ASP.NET Core 10, PostgreSQL, EF Core and an API-powered Kanban board. Role-based project access, milestones, builds, Docker and CI.

**Topics:** `dotnet`, `aspnetcore`, `csharp`, `postgresql`, `entity-framework-core`, `kanban`, `game-development`, `project-management`, `docker`, `rest-api`, `portfolio-project`, `javascript`

Create an **empty** public repository on GitHub (do not initialize with README / .gitignore / license), then from the unzipped project folder:

```powershell
git init
git branch -M main
git add .
git status --short
git commit -m "feat: scaffold PADO StudioOps production workspace"
git remote add origin https://github.com/PADOALEX/PADO-StudioOps.git
git push -u origin main
```

After pushing, verify GitHub Actions is green; only then capture real screenshots and add a CI badge. A suggested CV bullet, only after successful verification:

> Built PADO StudioOps, a full-stack game-studio management application with ASP.NET Core 10, EF Core/PostgreSQL, RBAC project scoping, Kanban drag-and-drop, Docker Compose and automated unit/integration tests.

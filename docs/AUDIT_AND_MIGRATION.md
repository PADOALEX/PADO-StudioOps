# Audit and feature migration map

## Source archive: `DB_Task.zip`

Primary schema: `DB_Task_2/00_Setup/01_schema_and_seed.sql`. Original schema creates all ten entities using native PostgreSQL identity columns, composite PKs on join tables, CHECK constraints, foreign keys, restrictive referential actions and indexed lookup FKs. The seed generates 100 studios, 300 employees/profiles, 200 game projects, 400 milestones and 1000 tasks, plus supporting catalog data. It is mass-generated classroom data, not appropriate as a believable product demo.

### SQL table → application entity → use

| Original table | EF entity | In-product use |
|---|---|---|
| `studios` | `Studio` | Studio CRUD and grouping |
| `employees` | `Employee` | Employee CRUD, project assignments |
| `employee_profiles` | `EmployeeProfile` | Stored schema compatibility / later employee profile expansion |
| `game_projects` | `GameProject` | Projects, studio ownership, statuses, budgets, release dates |
| `milestones` | `Milestone` | Roadmap CRUD, task grouping |
| `tasks` | `TaskItem` | Ticket CRUD, assignment, deadlines, Kanban workflow |
| `technologies` | `Technology` | Preserved catalog, API deferred |
| `game_builds` | `GameBuild` | Build history in overview, record new builds |
| `project_members` | `ProjectMember` | Authorized project scope and team list |
| `project_technologies` | `ProjectTechnology` | Preserved many-to-many relations, API deferred |
| *(new)* `app_users` | `AppUser` | Admin-provisioned credential and role |

### Preserved rules

- `studios.name`, `employees.email`, `game_projects.title`, `technologies.name` unique
- Nullable employee/studio FK uses `SET NULL`; studio/project cannot be deleted while referenced (`RESTRICT`)
- Composite keys on `project_members` and `project_technologies`
- Project, milestone, task statuses constrained to original values
- Non-negative salaries/budgets and strictly positive estimated hours
- Task assignee and project lead eligibility checked additionally in the API

### Intentional changes

- Original `founded_year BETWEEN 1970 AND 2026` adjusted to `1970..2100` for future use; validate historical plausibility in business rules as needed.
- `tasks` adds description, deadline, UTC creation/update timestamps and integer revision.
- `game_builds.build_date` uses timezone-aware timestamps for UTC correctness.
- Auth separated from employee HR fields; no password exposed in public DTOs.
- New demo is a plausible single studio / co-op horror game rather than thousands of generated rows.

### Dangerous SQL: excluded completely from runtime

`00_Setup/01_schema_and_seed.sql` includes `DROP SCHEMA IF EXISTS public CASCADE`; Task 4 scripts demonstrate deliberate transaction errors, rollbacks and audit triggers; Task 5 demonstrates DBA permissions, backup/restore and export to SQLite. None are executed by the app or copied to the Docker image.

## Source archive: `HTML-Test_fin.zip`

Original board: `HTML-Test/task-board.html`, `HTML-Test/task-manager.js`, `HTML-Test/styles.css`.

| Original feature | Migration |
|---|---|
| Board layout with three drag/drop columns | Retained as adapted `.task-board`, `.task-dropzone`, `.task-card` UI |
| DOM-generated ticket cards with priority badges | Retained and re-skinned |
| Modal create/edit/details | Replaced by accessible native `<dialog>` with task fields and DB-backed actions |
| Drag/drop move | `PATCH /api/projects/{id}/tasks/{taskId}/status` with revision |
| Search, priority filter | UI filtering plus API query capability |
| Local task counters | Calculated from API records + project overview aggregate |
| `localStorage` persistence / hard-coded sample tasks | **Removed**; seeded by explicit DB provisioning only |
| Other portfolio/contacts/event-manager pages | Excluded from standalone studio workspace |

## MVP acceptance

1. Application can bootstrap a fresh isolated PostgreSQL schema through EF migration.
2. Admin signs in, creates studio/employee/project and provisions staff account.
3. Project manager membership limits access to assigned projects.
4. Employee can only receive tasks if joined to that project.
5. Kanban move persists and survives refresh; stale revisions return HTTP 409.
6. Build history/milestones/progress reflect SQL-derived state.
7. Legacy destructive scripts never run in the application pipeline.

Not a claim of production readiness: see `docs/VERIFICATION.md` for actual environment constraints and required checks.

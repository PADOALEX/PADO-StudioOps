# Preserved laboratory material — **NOT application migrations**

The original PostgreSQL `DB_Task.zip` files are preserved verbatim under `DB_Task_2/` for learning and historical reference. They are **not** imported, executed, referenced or included as embedded resources in the StudioOps API, migrations, Docker image or CI.

**Danger:** `00_Setup/01_schema_and_seed.sql` starts with `DROP SCHEMA IF EXISTS public CASCADE`. Other exercises demonstrate rollback, deliberate errors, triggers, privileges, backups and SQLite conversion. Do not run any of these scripts against the application database. If experimenting, use a fresh, disposable PostgreSQL instance with separate credentials and a separate database.

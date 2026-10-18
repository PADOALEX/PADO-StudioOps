-- Run as postgres/database administrator in game_dev_studio_db.
-- NOLOGIN roles are permission groups; use SET ROLE to demonstrate.
-- Does not remove privileges from existing users or change existing tables.
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'task5_reader') THEN
        CREATE ROLE task5_reader NOLOGIN;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'task5_task_editor') THEN
        CREATE ROLE task5_task_editor NOLOGIN;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'task5_assignee1') THEN
        CREATE ROLE task5_assignee1 NOLOGIN;
    END IF;
END;
$$;

GRANT CONNECT ON DATABASE game_dev_studio_db
    TO task5_reader, task5_task_editor, task5_assignee1;
GRANT USAGE ON SCHEMA public
    TO task5_reader, task5_task_editor, task5_assignee1;

-- Reader sees public project information, not employees/salaries.
GRANT SELECT ON public.studios, public.game_projects,
                public.milestones, public.technologies
    TO task5_reader;

-- Editor may read tasks and change only TWO columns.
GRANT SELECT ON public.tasks TO task5_task_editor;
GRANT UPDATE (task_status, estimated_hours) ON public.tasks TO task5_task_editor;
-- Task 4 triggers insert audit entries with invoker privileges:
GRANT INSERT ON public.task_row_audit, public.task_statement_audit
    TO task5_task_editor;

-- Row-restricted read access implemented with a view; no RLS changes
-- to the existing tasks table.
CREATE OR REPLACE VIEW public.task5_assignee1_tasks
WITH (security_barrier = true)
AS
SELECT task_id, assigned_employee_id, title, task_status, estimated_hours
FROM public.tasks
WHERE assigned_employee_id = 1;

GRANT SELECT ON public.task5_assignee1_tasks TO task5_assignee1;

SELECT rolname, rolcanlogin, rolsuper
FROM pg_roles
WHERE rolname IN ('task5_reader', 'task5_task_editor', 'task5_assignee1')
ORDER BY rolname;

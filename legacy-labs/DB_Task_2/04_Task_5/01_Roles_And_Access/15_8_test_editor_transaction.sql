-- Run in one Query Tool: illustrates allowed status UPDATE.
-- It is rolled back to leave live rows unchanged.
BEGIN;
SET LOCAL ROLE task5_task_editor;
SELECT current_user AS effective_role;
UPDATE public.tasks
SET task_status = 'Testing'
WHERE task_id = (SELECT MIN(task_id) FROM public.tasks);
SELECT task_id, task_status FROM public.tasks ORDER BY task_id LIMIT 1;
ROLLBACK;
-- Note: a rollback also cancels the audit records created by Task 4 triggers.
-- The title column is prohibited by 15_6_editor_permissions.sql.

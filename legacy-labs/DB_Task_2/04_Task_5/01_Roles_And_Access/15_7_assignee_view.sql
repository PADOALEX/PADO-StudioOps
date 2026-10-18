-- Run in one Query Tool, execute entire block (no intentional errors).
SET ROLE task5_assignee1;
SELECT current_user AS effective_role;
SELECT task_id, assigned_employee_id, title, task_status
FROM public.task5_assignee1_tasks
ORDER BY task_id LIMIT 10;
SELECT COUNT(*) AS visible_assigned_tasks FROM public.task5_assignee1_tasks;
RESET ROLE;
-- The role cannot SELECT FROM public.tasks; only the filtered view.

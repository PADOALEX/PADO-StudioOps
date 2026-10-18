-- Run as postgres (no change to live data). Confirms column-level UPDATE.
SELECT
    has_column_privilege('task5_task_editor', 'public.tasks', 'task_status', 'UPDATE') AS can_update_status,
    has_column_privilege('task5_task_editor', 'public.tasks', 'estimated_hours', 'UPDATE') AS can_update_hours,
    has_column_privilege('task5_task_editor', 'public.tasks', 'title', 'UPDATE') AS can_update_title,
    has_table_privilege('task5_task_editor', 'public.employees', 'UPDATE') AS can_update_employees;

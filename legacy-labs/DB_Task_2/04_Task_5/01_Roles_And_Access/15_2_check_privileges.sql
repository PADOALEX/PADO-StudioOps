-- Run as postgres (administrator).
SELECT role_name,
       has_table_privilege(role_name, 'public.studios', 'SELECT') AS can_read_studios,
       has_table_privilege(role_name, 'public.employees', 'SELECT') AS can_read_all_employees,
       has_table_privilege(role_name, 'public.tasks', 'SELECT') AS can_read_tasks,
       has_column_privilege(role_name, 'public.tasks', 'task_status', 'UPDATE') AS can_update_task_status,
       has_column_privilege(role_name, 'public.tasks', 'title', 'UPDATE') AS can_update_task_title,
       has_table_privilege(role_name, 'public.task5_assignee1_tasks', 'SELECT') AS can_read_assignee1_view
FROM (VALUES ('task5_reader'), ('task5_task_editor'), ('task5_assignee1')) AS r(role_name)
ORDER BY role_name;

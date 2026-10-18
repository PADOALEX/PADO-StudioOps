-- Run in game_dev_studio_db, the untouched original.
SELECT current_database() AS database_name,
       (SELECT COUNT(*) FROM public.studios) AS studio_count,
       (SELECT COUNT(*) FROM public.employees) AS employee_count,
       (SELECT COUNT(*) FROM public.tasks) AS task_count,
       (SELECT COUNT(*) FROM public.game_projects) AS project_count;

-- Run in game_dev_studio_task5_restored AND in game_dev_studio_task5_recovered.
SELECT current_database() AS database_name,
       (SELECT COUNT(*) FROM public.studios) AS studio_count,
       (SELECT COUNT(*) FROM public.employees) AS employee_count,
       (SELECT COUNT(*) FROM public.tasks) AS task_count,
       (SELECT COUNT(*) FROM public.game_projects) AS project_count;
-- Keys and indices should also exist in the recovered database:
SELECT indexname, tablename
FROM pg_indexes
WHERE schemaname = 'public'
  AND tablename IN ('tasks', 'employees', 'game_projects')
ORDER BY tablename, indexname;

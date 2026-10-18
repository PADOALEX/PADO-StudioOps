DROP INDEX IF EXISTS ix_tasks_title_task2;

ANALYZE tasks;

SELECT 'WITHOUT INDEX' AS test_case;

EXPLAIN (ANALYZE, BUFFERS)
SELECT
    t1.task_id AS first_task_id,
    t2.task_id AS second_task_id,
    t1.title
FROM tasks t1
INNER JOIN tasks t2
    ON t1.title = t2.title
WHERE t1.title = 'Task 0999';

CREATE INDEX ix_tasks_title_task2
    ON tasks(title);

ANALYZE tasks;

SELECT 'WITH INDEX' AS test_case;

EXPLAIN (ANALYZE, BUFFERS)
SELECT
    t1.task_id AS first_task_id,
    t2.task_id AS second_task_id,
    t1.title
FROM tasks t1
INNER JOIN tasks t2
    ON t1.title = t2.title
WHERE t1.title = 'Task 0999';

SELECT
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'public'
  AND tablename = 'tasks'
  AND indexname = 'ix_tasks_title_task2';

-- SQLite queries: open output/game_dev_studio_sqlite.db in a SQLite DB tool.
-- In VS Code use a SQLite extension or run python 20_sqlite_queries.py.
-- Run with SQLite syntax (not PostgreSQL Query Tool).

SELECT COUNT(*) AS studio_count FROM studios;
SELECT COUNT(*) AS employee_count FROM employees;
SELECT COUNT(*) AS task_count FROM tasks;
SELECT COUNT(*) AS project_count FROM game_projects;

SELECT gp.project_id, gp.title AS project, s.name AS studio,
       e.first_name || ' ' || e.last_name AS leader
FROM game_projects AS gp
JOIN studios AS s ON s.studio_id = gp.studio_id
LEFT JOIN employees AS e ON e.employee_id = gp.project_lead_id
ORDER BY gp.project_id LIMIT 10;

SELECT t.task_id, t.title, m.name AS milestone, gp.title AS project
FROM tasks t
JOIN milestones m ON m.milestone_id = t.milestone_id
JOIN game_projects gp ON gp.project_id = m.project_id
ORDER BY t.task_id LIMIT 10;

PRAGMA foreign_key_check;
PRAGMA integrity_check;
PRAGMA foreign_key_list('tasks');
PRAGMA index_list('tasks');
PRAGMA table_info('tasks');

SELECT type, name, tbl_name
FROM sqlite_master
WHERE type IN ('table', 'index', 'trigger')
ORDER BY type, name;

SELECT 'studios' AS table_name, COUNT(*) AS record_count FROM studios
UNION ALL
SELECT 'employees', COUNT(*) FROM employees
UNION ALL
SELECT 'employee_profiles', COUNT(*) FROM employee_profiles
UNION ALL
SELECT 'game_projects', COUNT(*) FROM game_projects
UNION ALL
SELECT 'milestones', COUNT(*) FROM milestones
UNION ALL
SELECT 'tasks', COUNT(*) FROM tasks
UNION ALL
SELECT 'technologies', COUNT(*) FROM technologies
UNION ALL
SELECT 'game_builds', COUNT(*) FROM game_builds
UNION ALL
SELECT 'project_members', COUNT(*) FROM project_members
UNION ALL
SELECT 'project_technologies', COUNT(*) FROM project_technologies
ORDER BY table_name;

INSERT INTO studios (name, country, founded_year, website)
VALUES
    ('JOIN Demo Empty Studio A', 'Ukraine', 2026, NULL),
    ('JOIN Demo Empty Studio B', 'Poland', 2026, NULL)
ON CONFLICT (name) DO NOTHING;

INSERT INTO employees
(
    studio_id,
    first_name,
    last_name,
    email,
    position,
    hire_date,
    salary
)
VALUES
    (NULL, 'JoinDemo', 'OrphanA', 'join.orphan.a@gamedev.local',
     'QA Engineer', DATE '2026-09-20', 1500.00),
    (NULL, 'JoinDemo', 'OrphanB', 'join.orphan.b@gamedev.local',
     'Game Designer', DATE '2026-09-20', 1600.00)
ON CONFLICT (email) DO NOTHING;

ANALYZE studios;
ANALYZE employees;

SELECT
    s.studio_id,
    s.name AS studio_name,
    e.employee_id,
    e.first_name,
    e.last_name
FROM studios s
INNER JOIN employees e
    ON e.studio_id = s.studio_id
ORDER BY s.studio_id, e.employee_id
LIMIT 20;

SELECT
    s.studio_id,
    s.name AS studio_name,
    e.employee_id,
    e.first_name,
    e.last_name
FROM studios s
LEFT JOIN employees e
    ON e.studio_id = s.studio_id
WHERE e.employee_id IS NULL
ORDER BY s.studio_id;

SELECT
    s.studio_id,
    s.name AS studio_name,
    e.employee_id,
    e.first_name,
    e.last_name,
    e.email
FROM studios s
RIGHT JOIN employees e
    ON e.studio_id = s.studio_id
WHERE s.studio_id IS NULL
ORDER BY e.employee_id;

SELECT
    s.studio_id,
    s.name AS studio_name,
    e.employee_id,
    e.first_name,
    e.last_name,
    e.email
FROM studios s
FULL OUTER JOIN employees e
    ON e.studio_id = s.studio_id
WHERE s.studio_id IS NULL
   OR e.employee_id IS NULL
ORDER BY s.studio_id NULLS LAST, e.employee_id NULLS LAST;

SELECT
    'INNER JOIN' AS join_type,
    COUNT(*) AS total_rows,
    COUNT(*) FILTER (WHERE s.studio_id IS NULL) AS missing_studio,
    COUNT(*) FILTER (WHERE e.employee_id IS NULL) AS missing_employee
FROM studios s
INNER JOIN employees e
    ON e.studio_id = s.studio_id

UNION ALL

SELECT
    'LEFT JOIN',
    COUNT(*),
    COUNT(*) FILTER (WHERE s.studio_id IS NULL),
    COUNT(*) FILTER (WHERE e.employee_id IS NULL)
FROM studios s
LEFT JOIN employees e
    ON e.studio_id = s.studio_id

UNION ALL

SELECT
    'RIGHT JOIN',
    COUNT(*),
    COUNT(*) FILTER (WHERE s.studio_id IS NULL),
    COUNT(*) FILTER (WHERE e.employee_id IS NULL)
FROM studios s
RIGHT JOIN employees e
    ON e.studio_id = s.studio_id

UNION ALL

SELECT
    'FULL OUTER JOIN',
    COUNT(*),
    COUNT(*) FILTER (WHERE s.studio_id IS NULL),
    COUNT(*) FILTER (WHERE e.employee_id IS NULL)
FROM studios s
FULL OUTER JOIN employees e
    ON e.studio_id = s.studio_id;

SELECT
    gp.project_id,
    gp.title AS project_title,
    s.studio_id,
    s.name AS studio_name
FROM game_projects gp
INNER JOIN studios s
    ON gp.studio_id = s.studio_id
ORDER BY gp.project_id
LIMIT 20;

SELECT
    t1.task_id AS task_1_id,
    t2.task_id AS task_2_id,
    t1.task_status
FROM tasks t1
INNER JOIN tasks t2
    ON t1.task_status = t2.task_status
   AND t1.task_id < t2.task_id
ORDER BY t1.task_id, t2.task_id
LIMIT 30;

SELECT
    s.studio_id,
    s.name AS studio_name,
    t.technology_id,
    t.name AS technology_name
FROM studios s
INNER JOIN technologies t
    ON TRUE
WHERE s.studio_id <= 3
  AND t.technology_id <= 3
ORDER BY s.studio_id, t.technology_id;

SELECT
    gp.project_id,
    gp.title AS project_title,
    s.name AS studio_name,
    e.employee_id,
    e.first_name || ' ' || e.last_name AS member_name,
    pm.role_in_project,
    tech.name AS technology_name,
    pt.purpose
FROM game_projects gp
INNER JOIN studios s
    ON s.studio_id = gp.studio_id
INNER JOIN project_members pm
    ON pm.project_id = gp.project_id
INNER JOIN employees e
    ON e.employee_id = pm.employee_id
INNER JOIN project_technologies pt
    ON pt.project_id = gp.project_id
INNER JOIN technologies tech
    ON tech.technology_id = pt.technology_id
ORDER BY gp.project_id, e.employee_id, tech.technology_id
LIMIT 50;

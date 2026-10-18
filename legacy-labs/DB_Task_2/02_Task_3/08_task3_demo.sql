SELECT COUNT(*) AS processed_projects
FROM project_statistics;

SELECT
    ps.studio_name,
    COUNT(*) AS project_count,
    SUM(ps.total_estimated_hours) AS total_hours,
    ROUND(AVG(ps.completion_percent), 2) AS average_completion_percent,
    MAX(ps.latest_build_date) AS latest_build_date
FROM project_statistics ps
GROUP BY ps.studio_name
ORDER BY project_count DESC, ps.studio_name
LIMIT 20;

SELECT
    project_id,
    project_title,
    completion_percent,
    CASE
        WHEN completion_percent >= 80 THEN 'Almost finished'
        WHEN completion_percent >= 50 THEN 'In progress'
        WHEN completion_percent > 0 THEN 'Early stage'
        ELSE 'No completed tasks'
    END AS progress_category
FROM project_statistics
ORDER BY project_id
LIMIT 30;

SELECT
    gp.project_id,
    gp.title,
    gp.budget,
    COUNT(pm.employee_id) AS member_count,
    ROUND(
        gp.budget / NULLIF(COUNT(pm.employee_id), 0),
        2
    ) AS budget_per_member
FROM game_projects gp
LEFT JOIN project_members pm
    ON pm.project_id = gp.project_id
GROUP BY gp.project_id, gp.title, gp.budget
ORDER BY gp.project_id
LIMIT 30;

SELECT
    project_id,
    project_title,
    COALESCE(latest_build_date::TEXT, 'No builds yet') AS latest_build
FROM project_statistics
ORDER BY project_id
LIMIT 30;

SELECT
    project_id,
    title,
    total_estimated_hours,
    task_completion_percent,
    derived_risk_level
FROM game_projects
ORDER BY project_id
LIMIT 30;

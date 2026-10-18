DROP TABLE IF EXISTS project_statistics;

CREATE TABLE project_statistics
(
    project_id                INTEGER PRIMARY KEY,
    project_title             VARCHAR(120) NOT NULL,
    studio_name               VARCHAR(100) NOT NULL,
    member_count              INTEGER NOT NULL,
    task_count                INTEGER NOT NULL,
    completed_task_count      INTEGER NOT NULL,
    delayed_milestone_count   INTEGER NOT NULL,
    total_estimated_hours     NUMERIC(12,2) NOT NULL,
    average_task_hours        NUMERIC(10,2) NOT NULL,
    completion_percent        NUMERIC(5,2) NOT NULL,
    stable_build_count        INTEGER NOT NULL,
    latest_build_date         TIMESTAMP,
    budget_per_member         NUMERIC(14,2),
    workload_level            VARCHAR(20) NOT NULL,
    calculated_at             TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_project_statistics_project
        FOREIGN KEY (project_id)
        REFERENCES game_projects(project_id)
        ON DELETE CASCADE
);

WITH task_stats AS
(
    SELECT
        gp.project_id,
        COUNT(t.task_id) AS task_count,
        COUNT(t.task_id) FILTER (WHERE t.task_status = 'Done') AS completed_task_count,
        COUNT(DISTINCT m.milestone_id)
            FILTER (WHERE m.milestone_status = 'Delayed') AS delayed_milestone_count,
        COALESCE(SUM(t.estimated_hours), 0.00) AS total_estimated_hours,
        COALESCE(AVG(t.estimated_hours), 0.00) AS average_task_hours
    FROM game_projects gp
    LEFT JOIN milestones m
        ON m.project_id = gp.project_id
    LEFT JOIN tasks t
        ON t.milestone_id = m.milestone_id
    GROUP BY gp.project_id
),
member_stats AS
(
    SELECT
        gp.project_id,
        COUNT(pm.employee_id) AS member_count
    FROM game_projects gp
    LEFT JOIN project_members pm
        ON pm.project_id = gp.project_id
    GROUP BY gp.project_id
),
build_stats AS
(
    SELECT
        gp.project_id,
        COUNT(gb.build_id) FILTER (WHERE gb.is_stable = TRUE) AS stable_build_count,
        MAX(gb.build_date) AS latest_build_date
    FROM game_projects gp
    LEFT JOIN game_builds gb
        ON gb.project_id = gp.project_id
    GROUP BY gp.project_id
),
processed AS
(
    SELECT
        gp.project_id,
        gp.title AS project_title,
        s.name AS studio_name,

        COALESCE(ms.member_count, 0)::INTEGER AS member_count,
        COALESCE(ts.task_count, 0)::INTEGER AS task_count,
        COALESCE(ts.completed_task_count, 0)::INTEGER AS completed_task_count,
        COALESCE(ts.delayed_milestone_count, 0)::INTEGER AS delayed_milestone_count,
        ROUND(COALESCE(ts.total_estimated_hours, 0.00), 2) AS total_estimated_hours,
        ROUND(COALESCE(ts.average_task_hours, 0.00), 2) AS average_task_hours,

        COALESCE(
            ROUND(
                COALESCE(ts.completed_task_count, 0) * 100.0 /
                NULLIF(COALESCE(ts.task_count, 0), 0),
                2
            ),
            0.00
        ) AS completion_percent,

        COALESCE(bs.stable_build_count, 0)::INTEGER AS stable_build_count,
        bs.latest_build_date,

        ROUND(
            gp.budget / NULLIF(COALESCE(ms.member_count, 0), 0),
            2
        ) AS budget_per_member,

        CASE
            WHEN COALESCE(ts.total_estimated_hours, 0) >= 150 THEN 'Very High'
            WHEN COALESCE(ts.total_estimated_hours, 0) >= 100 THEN 'High'
            WHEN COALESCE(ts.total_estimated_hours, 0) >= 50  THEN 'Medium'
            ELSE 'Low'
        END AS workload_level

    FROM game_projects gp
    INNER JOIN studios s
        ON s.studio_id = gp.studio_id
    LEFT JOIN task_stats ts
        ON ts.project_id = gp.project_id
    LEFT JOIN member_stats ms
        ON ms.project_id = gp.project_id
    LEFT JOIN build_stats bs
        ON bs.project_id = gp.project_id
)
INSERT INTO project_statistics
(
    project_id,
    project_title,
    studio_name,
    member_count,
    task_count,
    completed_task_count,
    delayed_milestone_count,
    total_estimated_hours,
    average_task_hours,
    completion_percent,
    stable_build_count,
    latest_build_date,
    budget_per_member,
    workload_level
)
SELECT
    project_id,
    project_title,
    studio_name,
    member_count,
    task_count,
    completed_task_count,
    delayed_milestone_count,
    total_estimated_hours,
    average_task_hours,
    completion_percent,
    stable_build_count,
    latest_build_date,
    budget_per_member,
    workload_level
FROM processed;

SELECT
    project_id,
    project_title,
    studio_name,
    member_count,
    task_count,
    completed_task_count,
    completion_percent,
    total_estimated_hours,
    stable_build_count,
    budget_per_member,
    workload_level
FROM project_statistics
ORDER BY project_id
LIMIT 30;

ALTER TABLE game_projects
    ADD COLUMN IF NOT EXISTS total_estimated_hours NUMERIC(12,2),
    ADD COLUMN IF NOT EXISTS task_completion_percent NUMERIC(5,2),
    ADD COLUMN IF NOT EXISTS derived_risk_level VARCHAR(20);

UPDATE game_projects gp
SET
    total_estimated_hours = ps.total_estimated_hours,
    task_completion_percent = ps.completion_percent,
    derived_risk_level =
        CASE
            WHEN gp.project_status IN ('Released', 'Cancelled') THEN 'Closed'
            WHEN ps.delayed_milestone_count > 0
                 AND ps.completion_percent < 50 THEN 'High'
            WHEN ps.delayed_milestone_count > 0
                 OR ps.completion_percent < 50 THEN 'Medium'
            ELSE 'Low'
        END
FROM project_statistics ps
WHERE ps.project_id = gp.project_id;

SELECT
    gp.project_id,
    gp.title,
    gp.project_status,
    gp.total_estimated_hours,
    gp.task_completion_percent,
    gp.derived_risk_level
FROM game_projects gp
ORDER BY gp.project_id
LIMIT 30;

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

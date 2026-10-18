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

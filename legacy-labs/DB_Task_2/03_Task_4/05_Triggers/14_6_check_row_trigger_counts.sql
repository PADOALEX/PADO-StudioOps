SELECT
    action_name,
    COUNT(*) AS row_trigger_calls
FROM task_row_audit
GROUP BY action_name
ORDER BY action_name;

SELECT
    audit_id,
    action_name,
    task_id,
    task_title,
    old_status,
    new_status,
    old_estimated_hours,
    new_estimated_hours
FROM task_row_audit
WHERE action_name = 'UPDATE'
ORDER BY audit_id;

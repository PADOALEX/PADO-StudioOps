SELECT
    statement_audit_id,
    action_name,
    affected_rows,
    description
FROM task_statement_audit
WHERE action_name = 'UPDATE'
ORDER BY statement_audit_id;

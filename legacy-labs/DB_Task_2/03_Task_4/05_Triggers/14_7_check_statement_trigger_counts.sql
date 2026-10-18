SELECT
    action_name,
    affected_rows,
    description
FROM task_statement_audit
ORDER BY statement_audit_id;

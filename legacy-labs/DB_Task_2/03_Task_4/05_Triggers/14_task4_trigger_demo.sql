BEGIN;

DELETE FROM task_row_audit;
DELETE FROM task_statement_audit;

INSERT INTO tasks
(
    milestone_id,
    assigned_employee_id,
    title,
    priority,
    task_status,
    estimated_hours
)
VALUES
(
    (SELECT MIN(milestone_id) FROM milestones),
    NULL,
    '[TASK4 DEMO] Trigger row 1',
    'Medium',
    'To Do',
    4.00
),
(
    (SELECT MIN(milestone_id) FROM milestones),
    NULL,
    '[TASK4 DEMO] Trigger row 2',
    'High',
    'To Do',
    6.00
),
(
    (SELECT MIN(milestone_id) FROM milestones),
    NULL,
    '[TASK4 DEMO] Trigger row 3',
    'Low',
    'To Do',
    2.00
);

UPDATE tasks
SET
    task_status = 'In Progress',
    estimated_hours = estimated_hours + 1.00
WHERE title IN
(
    '[TASK4 DEMO] Trigger row 1',
    '[TASK4 DEMO] Trigger row 2',
    '[TASK4 DEMO] Trigger row 3'
);

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
ORDER BY audit_id;

SELECT
    statement_audit_id,
    action_name,
    affected_rows,
    description
FROM task_statement_audit
ORDER BY statement_audit_id;

DELETE FROM tasks
WHERE title IN
(
    '[TASK4 DEMO] Trigger row 1',
    '[TASK4 DEMO] Trigger row 2',
    '[TASK4 DEMO] Trigger row 3'
);

SELECT
    action_name,
    COUNT(*) AS row_trigger_calls
FROM task_row_audit
GROUP BY action_name
ORDER BY action_name;

SELECT
    action_name,
    affected_rows,
    description
FROM task_statement_audit
ORDER BY statement_audit_id;

ROLLBACK;

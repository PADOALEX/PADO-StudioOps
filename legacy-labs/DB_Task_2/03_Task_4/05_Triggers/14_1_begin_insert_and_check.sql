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

SELECT
    audit_id,
    action_name,
    task_id,
    task_title,
    new_status
FROM task_row_audit
WHERE action_name = 'INSERT'
ORDER BY audit_id;

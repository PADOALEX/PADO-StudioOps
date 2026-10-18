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

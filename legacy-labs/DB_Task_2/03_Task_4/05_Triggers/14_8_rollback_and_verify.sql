ROLLBACK;

SELECT COUNT(*) AS demo_tasks_remaining
FROM tasks
WHERE title IN
(
    '[TASK4 DEMO] Trigger row 1',
    '[TASK4 DEMO] Trigger row 2',
    '[TASK4 DEMO] Trigger row 3'
);

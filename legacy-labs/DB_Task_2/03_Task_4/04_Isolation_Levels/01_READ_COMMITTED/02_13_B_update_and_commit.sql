BEGIN;

UPDATE task4_isolation_demo
SET demo_value = 200
WHERE demo_id = 1;

COMMIT;

SELECT demo_id, demo_value
FROM task4_isolation_demo
WHERE demo_id = 1;

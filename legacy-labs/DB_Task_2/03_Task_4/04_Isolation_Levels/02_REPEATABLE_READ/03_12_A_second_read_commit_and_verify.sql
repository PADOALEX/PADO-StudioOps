SELECT demo_id, project_id, demo_value, description
FROM task4_isolation_demo
WHERE demo_id = 1;

COMMIT;

SELECT demo_id, project_id, demo_value, description
FROM task4_isolation_demo
WHERE demo_id = 1;

UPDATE task4_isolation_demo
SET demo_value = 100
WHERE demo_id = 1;

SELECT employee_id, first_name, last_name, salary
FROM employees
ORDER BY employee_id
LIMIT 3;

BEGIN;

UPDATE employees
SET salary = salary + 100.00
WHERE employee_id =
(
    SELECT employee_id
    FROM employees
    ORDER BY employee_id
    LIMIT 1
);

SAVEPOINT risky_part;

UPDATE employees
SET salary = salary + 200.00
WHERE employee_id =
(
    SELECT employee_id
    FROM employees
    ORDER BY employee_id
    LIMIT 1 OFFSET 1
);

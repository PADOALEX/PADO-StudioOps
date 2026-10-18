UPDATE employees
SET salary = salary + 25.00
WHERE employee_id =
(
    SELECT employee_id
    FROM employees
    ORDER BY employee_id
    LIMIT 1 OFFSET 1
);

SELECT employee_id, first_name, last_name, salary
FROM employees
ORDER BY employee_id
LIMIT 3;

ROLLBACK;

SELECT employee_id, first_name, last_name, salary
FROM employees
ORDER BY employee_id
LIMIT 3;

UPDATE employees
SET salary = -1.00
WHERE employee_id =
(
    SELECT employee_id
    FROM employees
    ORDER BY employee_id
    LIMIT 1 OFFSET 2
);

SELECT employee_id, first_name, last_name, salary
FROM employees
WHERE employee_id = (SELECT MIN(employee_id) FROM employees);

BEGIN TRANSACTION ISOLATION LEVEL READ COMMITTED;

UPDATE employees
SET salary = salary + 500.00
WHERE employee_id = (SELECT MIN(employee_id) FROM employees);

SELECT employee_id, salary
FROM employees
WHERE employee_id = (SELECT MIN(employee_id) FROM employees);

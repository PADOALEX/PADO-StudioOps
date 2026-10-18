ROLLBACK;

SELECT employee_id, first_name, last_name, salary
FROM employees
WHERE employee_id = (SELECT MIN(employee_id) FROM employees);

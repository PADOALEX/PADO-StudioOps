ROLLBACK TO SAVEPOINT risky_part;

SELECT employee_id, first_name, last_name, salary
FROM employees
ORDER BY employee_id
LIMIT 3;

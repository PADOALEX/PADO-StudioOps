UPDATE employees
SET salary = -1.00
WHERE employee_id = (SELECT MIN(employee_id) FROM employees);

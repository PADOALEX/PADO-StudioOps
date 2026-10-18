SELECT
    e.employee_id,
    e.first_name,
    e.last_name,
    e.studio_id,
    e.position,
    e.salary,
    (
        SELECT ROUND(AVG(e2.salary), 2)
        FROM employees e2
        WHERE e2.studio_id = e.studio_id
    ) AS studio_average_salary
FROM employees e
WHERE e.studio_id IS NOT NULL
  AND e.salary >
    (
        SELECT AVG(e2.salary)
        FROM employees e2
        WHERE e2.studio_id = e.studio_id
    )
ORDER BY e.studio_id, e.salary DESC, e.employee_id
LIMIT 100;

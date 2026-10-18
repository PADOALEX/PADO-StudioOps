-- Run in the SAME Query Tool as 15_3. Expected error: permission denied.
SELECT employee_id, salary FROM public.employees LIMIT 3;
-- If the query errors, immediately run 15_5_reset_role.sql.

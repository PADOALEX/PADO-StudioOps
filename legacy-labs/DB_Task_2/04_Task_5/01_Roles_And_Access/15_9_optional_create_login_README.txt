OPTIONAL: Show a real LOGIN USER in pgAdmin without hardcoding a password in SQL.
1) Login/Group Roles > Create > Login/Group Role.
2) General: Name = task5_student_reader.
3) Definition: set a fresh password in pgAdmin (do not save in project files).
4) Privileges: Can login = Yes; Superuser = No; Create DB/role = No.
5) As postgres in Query Tool:
       GRANT task5_reader TO task5_student_reader;
6) Connect using task5_student_reader and run SELECT from public.studios.
   SELECT from public.employees should be denied.
Important: task5_reader and task5_task_editor are NOLOGIN group roles by design.

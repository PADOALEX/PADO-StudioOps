-- Run in ONE Query Tool session. Must be executed by a superuser/role member.
SET ROLE task5_reader;
SELECT current_user AS effective_role;
SELECT studio_id, name, country FROM public.studios ORDER BY studio_id LIMIT 5;
-- After this run 15_4_reader_denied.sql IN THE SAME TAB.

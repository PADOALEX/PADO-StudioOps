-- Run ONLY in game_dev_studio_task5_restored (the restored COPY).
-- Safety guard and DELETE are in ONE atomic DO block.
-- If you run it on the ORIGINAL DB, it raises an error BEFORE any DELETE.
DO $$
DECLARE
    v_before bigint;
    v_deleted int;
    v_after bigint;
BEGIN
    IF current_database() <> 'game_dev_studio_task5_restored' THEN
        RAISE EXCEPTION 'Safety stop: wrong database %. Connect to game_dev_studio_task5_restored.', current_database();
    END IF;

    SELECT COUNT(*) INTO v_before FROM public.tasks;

    DELETE FROM public.tasks
    WHERE task_id = (SELECT MIN(task_id) FROM public.tasks);
    GET DIAGNOSTICS v_deleted = ROW_COUNT;

    SELECT COUNT(*) INTO v_after FROM public.tasks;
    RAISE NOTICE 'Task count before: %, deleted: %, after: %', v_before, v_deleted, v_after;
END;
$$;

SELECT current_database() AS database_name, COUNT(*) AS task_count_after
FROM public.tasks;

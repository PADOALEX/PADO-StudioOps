CREATE TABLE IF NOT EXISTS task4_isolation_demo
(
    demo_id       INTEGER PRIMARY KEY,
    project_id    INTEGER NOT NULL,
    demo_value    INTEGER NOT NULL,
    description   VARCHAR(200) NOT NULL,

    CONSTRAINT fk_task4_isolation_project
        FOREIGN KEY (project_id)
        REFERENCES game_projects(project_id)
        ON DELETE CASCADE
);

INSERT INTO task4_isolation_demo (demo_id, project_id, demo_value, description)
VALUES
(
    1,
    (SELECT MIN(project_id) FROM game_projects),
    100,
    'Row used to compare READ COMMITTED and REPEATABLE READ'
)
ON CONFLICT (demo_id)
DO UPDATE SET
    project_id = EXCLUDED.project_id,
    demo_value = 100,
    description = EXCLUDED.description;

CREATE TABLE IF NOT EXISTS task_row_audit
(
    audit_id             BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    action_name          VARCHAR(10) NOT NULL,
    task_id              INTEGER NOT NULL,
    task_title           VARCHAR(160) NOT NULL,
    old_status           VARCHAR(30),
    new_status           VARCHAR(30),
    old_estimated_hours  NUMERIC(7,2),
    new_estimated_hours  NUMERIC(7,2),
    changed_at           TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    transaction_id       BIGINT NOT NULL DEFAULT txid_current(),

    CONSTRAINT ck_task_row_audit_action
        CHECK (action_name IN ('INSERT', 'UPDATE', 'DELETE'))
);

CREATE TABLE IF NOT EXISTS task_statement_audit
(
    statement_audit_id  BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    action_name         VARCHAR(10) NOT NULL,
    affected_rows       INTEGER NOT NULL,
    description         VARCHAR(250) NOT NULL,
    changed_at          TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    transaction_id      BIGINT NOT NULL DEFAULT txid_current(),

    CONSTRAINT ck_task_statement_audit_action
        CHECK (action_name IN ('UPDATE', 'DELETE')),
    CONSTRAINT ck_task_statement_audit_rows
        CHECK (affected_rows >= 0)
);

CREATE OR REPLACE FUNCTION fn_tasks_row_audit()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        INSERT INTO task_row_audit
        (
            action_name,
            task_id,
            task_title,
            old_status,
            new_status,
            old_estimated_hours,
            new_estimated_hours
        )
        VALUES
        (
            TG_OP,
            NEW.task_id,
            NEW.title,
            NULL,
            NEW.task_status,
            NULL,
            NEW.estimated_hours
        );

        RETURN NEW;

    ELSIF TG_OP = 'UPDATE' THEN
        INSERT INTO task_row_audit
        (
            action_name,
            task_id,
            task_title,
            old_status,
            new_status,
            old_estimated_hours,
            new_estimated_hours
        )
        VALUES
        (
            TG_OP,
            NEW.task_id,
            NEW.title,
            OLD.task_status,
            NEW.task_status,
            OLD.estimated_hours,
            NEW.estimated_hours
        );

        RETURN NEW;

    ELSIF TG_OP = 'DELETE' THEN
        INSERT INTO task_row_audit
        (
            action_name,
            task_id,
            task_title,
            old_status,
            new_status,
            old_estimated_hours,
            new_estimated_hours
        )
        VALUES
        (
            TG_OP,
            OLD.task_id,
            OLD.title,
            OLD.task_status,
            NULL,
            OLD.estimated_hours,
            NULL
        );

        RETURN OLD;
    END IF;

    RETURN NULL;
END;
$$;

DROP TRIGGER IF EXISTS trg_tasks_row_audit ON tasks;

CREATE TRIGGER trg_tasks_row_audit
AFTER INSERT OR UPDATE OR DELETE
ON tasks
FOR EACH ROW
EXECUTE FUNCTION fn_tasks_row_audit();

CREATE OR REPLACE FUNCTION fn_tasks_update_statement_audit()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
DECLARE
    v_affected_rows INTEGER;
BEGIN
    SELECT COUNT(*)
    INTO v_affected_rows
    FROM task4_updated_rows;

    INSERT INTO task_statement_audit
    (
        action_name,
        affected_rows,
        description
    )
    VALUES
    (
        TG_OP,
        v_affected_rows,
        'One FOR EACH STATEMENT handler processed the whole UPDATE statement.'
    );

    RETURN NULL;
END;
$$;

DROP TRIGGER IF EXISTS trg_tasks_update_statement_audit ON tasks;

CREATE TRIGGER trg_tasks_update_statement_audit
AFTER UPDATE
ON tasks
REFERENCING NEW TABLE AS task4_updated_rows
FOR EACH STATEMENT
EXECUTE FUNCTION fn_tasks_update_statement_audit();

CREATE OR REPLACE FUNCTION fn_tasks_delete_statement_audit()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
DECLARE
    v_affected_rows INTEGER;
BEGIN
    SELECT COUNT(*)
    INTO v_affected_rows
    FROM task4_deleted_rows;

    INSERT INTO task_statement_audit
    (
        action_name,
        affected_rows,
        description
    )
    VALUES
    (
        TG_OP,
        v_affected_rows,
        'One FOR EACH STATEMENT handler processed the whole DELETE statement.'
    );

    RETURN NULL;
END;
$$;

DROP TRIGGER IF EXISTS trg_tasks_delete_statement_audit ON tasks;

CREATE TRIGGER trg_tasks_delete_statement_audit
AFTER DELETE
ON tasks
REFERENCING OLD TABLE AS task4_deleted_rows
FOR EACH STATEMENT
EXECUTE FUNCTION fn_tasks_delete_statement_audit();

SELECT *
FROM task4_isolation_demo
ORDER BY demo_id;

SELECT
    trigger_name,
    event_manipulation,
    action_timing,
    action_orientation
FROM information_schema.triggers
WHERE event_object_table = 'tasks'
  AND trigger_name LIKE 'trg_tasks_%'
ORDER BY trigger_name, event_manipulation;

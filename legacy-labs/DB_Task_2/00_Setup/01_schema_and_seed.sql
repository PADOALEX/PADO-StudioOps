DROP SCHEMA IF EXISTS public CASCADE;
CREATE SCHEMA public;

CREATE TABLE studios
(
    studio_id      INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name           VARCHAR(100) NOT NULL UNIQUE,
    country        VARCHAR(60)  NOT NULL,
    founded_year   SMALLINT     NOT NULL,
    website        VARCHAR(200),

    CONSTRAINT ck_studios_founded_year
        CHECK (founded_year BETWEEN 1970 AND 2026)
);

CREATE TABLE employees
(
    employee_id    INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    studio_id      INTEGER,
    first_name     VARCHAR(50)  NOT NULL,
    last_name      VARCHAR(50)  NOT NULL,
    email          VARCHAR(120) NOT NULL UNIQUE,
    position       VARCHAR(80)  NOT NULL,
    hire_date      DATE         NOT NULL,
    salary         NUMERIC(12,2) NOT NULL,

    CONSTRAINT ck_employees_salary
        CHECK (salary >= 0),

    CONSTRAINT fk_employees_studios
        FOREIGN KEY (studio_id)
        REFERENCES studios(studio_id)
        ON DELETE SET NULL
);

CREATE TABLE employee_profiles
(
    employee_id    INTEGER PRIMARY KEY,
    birth_date     DATE         NOT NULL,
    phone          VARCHAR(30),
    city           VARCHAR(80),
    bio            VARCHAR(500),

    CONSTRAINT fk_employee_profiles_employees
        FOREIGN KEY (employee_id)
        REFERENCES employees(employee_id)
        ON DELETE CASCADE
);

CREATE TABLE game_projects
(
    project_id            INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    studio_id             INTEGER NOT NULL,
    project_lead_id       INTEGER,
    title                 VARCHAR(120) NOT NULL UNIQUE,
    genre                 VARCHAR(60)  NOT NULL,
    project_status        VARCHAR(30)  NOT NULL,
    start_date            DATE         NOT NULL,
    planned_release_date  DATE,
    budget                NUMERIC(14,2) NOT NULL,

    CONSTRAINT ck_game_projects_budget
        CHECK (budget >= 0),

    CONSTRAINT ck_game_projects_status
        CHECK (
            project_status IN
            ('Concept', 'Pre-production', 'Production', 'Testing', 'Released', 'Cancelled')
        ),

    CONSTRAINT fk_game_projects_studios
        FOREIGN KEY (studio_id)
        REFERENCES studios(studio_id)
        ON DELETE RESTRICT,

    CONSTRAINT fk_game_projects_project_lead
        FOREIGN KEY (project_lead_id)
        REFERENCES employees(employee_id)
        ON DELETE SET NULL
);

CREATE TABLE milestones
(
    milestone_id       INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    project_id         INTEGER NOT NULL,
    name               VARCHAR(120) NOT NULL,
    due_date           DATE NOT NULL,
    milestone_status   VARCHAR(30) NOT NULL,

    CONSTRAINT ck_milestones_status
        CHECK (milestone_status IN ('Planned', 'In Progress', 'Completed', 'Delayed')),

    CONSTRAINT fk_milestones_game_projects
        FOREIGN KEY (project_id)
        REFERENCES game_projects(project_id)
        ON DELETE CASCADE
);

CREATE TABLE tasks
(
    task_id                INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    milestone_id           INTEGER NOT NULL,
    assigned_employee_id   INTEGER,
    title                  VARCHAR(160) NOT NULL,
    priority               VARCHAR(20) NOT NULL,
    task_status            VARCHAR(30) NOT NULL,
    estimated_hours        NUMERIC(7,2) NOT NULL,

    CONSTRAINT ck_tasks_priority
        CHECK (priority IN ('Low', 'Medium', 'High', 'Critical')),

    CONSTRAINT ck_tasks_status
        CHECK (task_status IN ('To Do', 'In Progress', 'Code Review', 'Testing', 'Done')),

    CONSTRAINT ck_tasks_estimated_hours
        CHECK (estimated_hours > 0),

    CONSTRAINT fk_tasks_milestones
        FOREIGN KEY (milestone_id)
        REFERENCES milestones(milestone_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_tasks_assigned_employee
        FOREIGN KEY (assigned_employee_id)
        REFERENCES employees(employee_id)
        ON DELETE SET NULL
);

CREATE TABLE technologies
(
    technology_id   INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name            VARCHAR(100) NOT NULL UNIQUE,
    category        VARCHAR(60)  NOT NULL,
    version         VARCHAR(30)
);

CREATE TABLE game_builds
(
    build_id                INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    project_id              INTEGER NOT NULL,
    created_by_employee_id  INTEGER,
    version                 VARCHAR(30) NOT NULL,
    platform                VARCHAR(40) NOT NULL,
    build_date              TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_stable               BOOLEAN NOT NULL DEFAULT FALSE,

    CONSTRAINT fk_game_builds_game_projects
        FOREIGN KEY (project_id)
        REFERENCES game_projects(project_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_game_builds_employees
        FOREIGN KEY (created_by_employee_id)
        REFERENCES employees(employee_id)
        ON DELETE SET NULL
);

CREATE TABLE project_members
(
    project_id       INTEGER NOT NULL,
    employee_id      INTEGER NOT NULL,
    role_in_project  VARCHAR(80) NOT NULL,
    joined_at        DATE NOT NULL,

    CONSTRAINT pk_project_members
        PRIMARY KEY (project_id, employee_id),

    CONSTRAINT fk_project_members_game_projects
        FOREIGN KEY (project_id)
        REFERENCES game_projects(project_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_project_members_employees
        FOREIGN KEY (employee_id)
        REFERENCES employees(employee_id)
        ON DELETE RESTRICT
);

CREATE TABLE project_technologies
(
    project_id       INTEGER NOT NULL,
    technology_id    INTEGER NOT NULL,
    purpose          VARCHAR(120) NOT NULL,

    CONSTRAINT pk_project_technologies
        PRIMARY KEY (project_id, technology_id),

    CONSTRAINT fk_project_technologies_game_projects
        FOREIGN KEY (project_id)
        REFERENCES game_projects(project_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_project_technologies_technologies
        FOREIGN KEY (technology_id)
        REFERENCES technologies(technology_id)
        ON DELETE RESTRICT
);

CREATE INDEX ix_employees_studio_id
    ON employees(studio_id);

CREATE INDEX ix_game_projects_studio_id
    ON game_projects(studio_id);

CREATE INDEX ix_game_projects_project_lead_id
    ON game_projects(project_lead_id);

CREATE INDEX ix_milestones_project_id
    ON milestones(project_id);

CREATE INDEX ix_tasks_milestone_id
    ON tasks(milestone_id);

CREATE INDEX ix_tasks_assigned_employee_id
    ON tasks(assigned_employee_id);

CREATE INDEX ix_game_builds_project_id
    ON game_builds(project_id);

INSERT INTO studios (name, country, founded_year, website)
SELECT
    'Studio ' || LPAD(n::text, 3, '0'),
    CASE n % 5
        WHEN 0 THEN 'Ukraine'
        WHEN 1 THEN 'Poland'
        WHEN 2 THEN 'Germany'
        WHEN 3 THEN 'Canada'
        ELSE 'United Kingdom'
    END,
    (1990 + (n % 35))::SMALLINT,
    'https://studio' || n || '.example.com'
FROM generate_series(1, 100) AS g(n);

INSERT INTO employees
(
    studio_id,
    first_name,
    last_name,
    email,
    position,
    hire_date,
    salary
)
SELECT
    ((n - 1) % 100) + 1,
    'EmployeeName' || n,
    'EmployeeSurname' || n,
    'employee' || n || '@gamedev.local',
    CASE n % 6
        WHEN 0 THEN 'Game Designer'
        WHEN 1 THEN 'Gameplay Programmer'
        WHEN 2 THEN '3D Artist'
        WHEN 3 THEN 'QA Engineer'
        WHEN 4 THEN 'Producer'
        ELSE 'Level Designer'
    END,
    DATE '2026-09-01' - (n * 3),
    (900 + (n % 120) * 25)::NUMERIC(12,2)
FROM generate_series(1, 300) AS g(n);

INSERT INTO employee_profiles
(
    employee_id,
    birth_date,
    phone,
    city,
    bio
)
SELECT
    n,
    make_date(1980 + (n % 25), 1 + (n % 12), 1 + (n % 28)),
    '+38067' || LPAD(n::text, 7, '0'),
    CASE n % 5
        WHEN 0 THEN 'Lviv'
        WHEN 1 THEN 'Kyiv'
        WHEN 2 THEN 'Warsaw'
        WHEN 3 THEN 'Berlin'
        ELSE 'Toronto'
    END,
    'Profile information for employee #' || n
FROM generate_series(1, 300) AS g(n);

INSERT INTO game_projects
(
    studio_id,
    project_lead_id,
    title,
    genre,
    project_status,
    start_date,
    planned_release_date,
    budget
)
SELECT
    ((n - 1) % 100) + 1,
    (((n - 1) % 100) + 1) + (100 * ((n - 1) % 3)),
    'Game Project ' || LPAD(n::text, 3, '0'),
    CASE n % 6
        WHEN 0 THEN 'Action'
        WHEN 1 THEN 'RPG'
        WHEN 2 THEN 'Strategy'
        WHEN 3 THEN 'Horror'
        WHEN 4 THEN 'Puzzle'
        ELSE 'Simulation'
    END,
    CASE n % 6
        WHEN 0 THEN 'Concept'
        WHEN 1 THEN 'Pre-production'
        WHEN 2 THEN 'Production'
        WHEN 3 THEN 'Testing'
        WHEN 4 THEN 'Released'
        ELSE 'Cancelled'
    END,
    DATE '2026-09-01' - (n * 5),
    DATE '2026-09-01' + (180 + n),
    (50000 + n * 10000)::NUMERIC(14,2)
FROM generate_series(1, 200) AS g(n);

INSERT INTO milestones
(
    project_id,
    name,
    due_date,
    milestone_status
)
SELECT
    ((n - 1) % 200) + 1,
    'Milestone ' || n,
    DATE '2026-09-01' + n,
    CASE n % 4
        WHEN 0 THEN 'Planned'
        WHEN 1 THEN 'In Progress'
        WHEN 2 THEN 'Completed'
        ELSE 'Delayed'
    END
FROM generate_series(1, 400) AS g(n);

INSERT INTO tasks
(
    milestone_id,
    assigned_employee_id,
    title,
    priority,
    task_status,
    estimated_hours
)
SELECT
    ((n - 1) % 400) + 1,
    ((n - 1) % 300) + 1,
    'Task ' || LPAD(n::text, 4, '0'),
    CASE n % 4
        WHEN 0 THEN 'Low'
        WHEN 1 THEN 'Medium'
        WHEN 2 THEN 'High'
        ELSE 'Critical'
    END,
    CASE n % 5
        WHEN 0 THEN 'To Do'
        WHEN 1 THEN 'In Progress'
        WHEN 2 THEN 'Code Review'
        WHEN 3 THEN 'Testing'
        ELSE 'Done'
    END,
    (1 + (n % 80) * 0.5)::NUMERIC(7,2)
FROM generate_series(1, 1000) AS g(n);

INSERT INTO technologies
(
    name,
    category,
    version
)
SELECT
    'Technology ' || LPAD(n::text, 3, '0'),
    CASE n % 5
        WHEN 0 THEN 'Game Engine'
        WHEN 1 THEN 'Programming Language'
        WHEN 2 THEN 'Database'
        WHEN 3 THEN 'Graphics Tool'
        ELSE 'Backend Service'
    END,
    'v' || (1 + (n % 5)) || '.' || (n % 10)
FROM generate_series(1, 100) AS g(n);

INSERT INTO game_builds
(
    project_id,
    created_by_employee_id,
    version,
    platform,
    build_date,
    is_stable
)
SELECT
    ((n - 1) % 200) + 1,
    ((n - 1) % 300) + 1,
    '0.' || ((n / 20) + 1) || '.' || (n % 20),
    CASE n % 4
        WHEN 0 THEN 'Windows'
        WHEN 1 THEN 'Linux'
        WHEN 2 THEN 'Android'
        ELSE 'iOS'
    END,
    TIMESTAMP '2026-09-01 12:00:00' - make_interval(days => (n % 180)),
    (n % 3 = 0)
FROM generate_series(1, 300) AS g(n);

WITH candidate_pairs AS
(
    SELECT
        p.project_id,
        e.employee_id,
        ROW_NUMBER() OVER (ORDER BY p.project_id, e.employee_id) AS rn
    FROM game_projects p
    CROSS JOIN employees e
    WHERE (p.project_id + e.employee_id) % 7 = 0
)
INSERT INTO project_members
(
    project_id,
    employee_id,
    role_in_project,
    joined_at
)
SELECT
    project_id,
    employee_id,
    CASE rn % 5
        WHEN 0 THEN 'Programmer'
        WHEN 1 THEN 'Game Designer'
        WHEN 2 THEN 'Artist'
        WHEN 3 THEN 'QA'
        ELSE 'Producer'
    END,
    DATE '2026-09-01' - ((rn % 365)::INTEGER)
FROM candidate_pairs
WHERE rn <= 600;

WITH candidate_pairs AS
(
    SELECT
        p.project_id,
        t.technology_id,
        ROW_NUMBER() OVER (ORDER BY p.project_id, t.technology_id) AS rn
    FROM game_projects p
    CROSS JOIN technologies t
)
INSERT INTO project_technologies
(
    project_id,
    technology_id,
    purpose
)
SELECT
    project_id,
    technology_id,
    CASE rn % 4
        WHEN 0 THEN 'Core development'
        WHEN 1 THEN 'Art pipeline'
        WHEN 2 THEN 'Data storage'
        ELSE 'Build and deployment'
    END
FROM candidate_pairs
WHERE rn <= 500;

SELECT 'studios' AS table_name, COUNT(*) AS record_count FROM studios
UNION ALL
SELECT 'employees', COUNT(*) FROM employees
UNION ALL
SELECT 'employee_profiles', COUNT(*) FROM employee_profiles
UNION ALL
SELECT 'game_projects', COUNT(*) FROM game_projects
UNION ALL
SELECT 'milestones', COUNT(*) FROM milestones
UNION ALL
SELECT 'tasks', COUNT(*) FROM tasks
UNION ALL
SELECT 'technologies', COUNT(*) FROM technologies
UNION ALL
SELECT 'game_builds', COUNT(*) FROM game_builds
UNION ALL
SELECT 'project_members', COUNT(*) FROM project_members
UNION ALL
SELECT 'project_technologies', COUNT(*) FROM project_technologies
ORDER BY table_name;

SELECT
    gp.project_id,
    gp.title AS project,
    s.name AS studio,
    e.first_name || ' ' || e.last_name AS project_lead,
    gp.project_status,
    gp.budget
FROM game_projects gp
JOIN studios s
    ON s.studio_id = gp.studio_id
LEFT JOIN employees e
    ON e.employee_id = gp.project_lead_id
ORDER BY gp.project_id
LIMIT 20;

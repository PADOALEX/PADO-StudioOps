DROP TABLE IF EXISTS PersonChangesLog;

CREATE TABLE PersonChangesLog
(
    Id            INTEGER PRIMARY KEY,
    PersonId      INTEGER NOT NULL,
    Name          VARCHAR(50) NOT NULL,
    Position      VARCHAR(50) NOT NULL,
    Salary        INTEGER NOT NULL,
    EnglishLevel  VARCHAR(3) NOT NULL,
    ManagerId     INTEGER,
    CVLink        VARCHAR(200) NOT NULL,
    EditTimestamp TIMESTAMP NOT NULL,

    CONSTRAINT ck_person_changes_english_level
        CHECK (EnglishLevel IN ('A1', 'A2', 'B1', 'B2', 'C1', 'C2'))
);

INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (1, 5, 'Eve', 'Dev', 52697, 'A1', 10, 'http://cv.example.com/5', TIMESTAMP '2022-02-01 05:46:27');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (2, 9, 'Ian', 'Designer', 33288, 'A2', 8, 'http://cv.example.com/9', TIMESTAMP '2023-07-22 06:29:14');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (3, 2, 'Bob', 'Manager', 60064, 'A2', 6, 'http://cv.example.com/2', TIMESTAMP '2023-07-22 07:00:43');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (4, 8, 'Helen', 'Manager', 60110, 'A1', NULL, 'http://cv.example.com/8', TIMESTAMP '2021-01-08 20:24:46');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (5, 10, 'Jane', 'Designer', 81137, 'A1', 5, 'http://cv.example.com/10', TIMESTAMP '2023-11-09 10:20:10');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (6, 10, 'Jane', 'Manager', 64101, 'B2', NULL, 'http://cv.example.com/10', TIMESTAMP '2023-08-08 00:02:27');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (7, 10, 'Jane', 'Manager', 59000, 'B1', 10, 'http://cv.example.com/10', TIMESTAMP '2023-04-09 11:12:12');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (8, 5, 'Eve', 'Dev', 85910, 'B1', 6, 'http://cv.example.com/5', TIMESTAMP '2023-06-13 23:17:49');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (9, 6, 'Frank', 'Dev', 45937, 'A2', 6, 'http://cv.example.com/6', TIMESTAMP '2023-01-02 03:54:32');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (10, 4, 'Diana', 'Analyst', 76756, 'C1', 6, 'http://cv.example.com/4', TIMESTAMP '2023-06-23 06:41:06');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (11, 7, 'Grace', 'Manager', 87395, 'C1', NULL, 'http://cv.example.com/7', TIMESTAMP '2023-03-24 21:56:58');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (12, 10, 'Jane', 'Dev', 33363, 'A2', 10, 'http://cv.example.com/10', TIMESTAMP '2023-07-03 15:31:15');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (13, 3, 'Charlie', 'Manager', 35107, 'A2', 2, 'http://cv.example.com/3', TIMESTAMP '2023-10-26 15:59:16');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (14, 4, 'Diana', 'Manager', 66717, 'C1', 3, 'http://cv.example.com/4', TIMESTAMP '2024-07-03 02:35:42');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (15, 7, 'Grace', 'Designer', 66467, 'C2', 5, 'http://cv.example.com/7', TIMESTAMP '2023-07-24 08:14:59');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (16, 9, 'Ian', 'QA', 79984, 'C1', 1, 'http://cv.example.com/9', TIMESTAMP '2023-11-13 03:11:01');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (17, 8, 'Helen', 'Manager', 67945, 'C1', NULL, 'http://cv.example.com/8', TIMESTAMP '2025-10-20 15:02:57');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (18, 5, 'Eve', 'QA', 71646, 'A1', 1, 'http://cv.example.com/5', TIMESTAMP '2020-11-26 02:59:32');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (19, 4, 'Diana', 'QA', 82140, 'A1', 9, 'http://cv.example.com/4', TIMESTAMP '2023-08-02 00:17:40');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (20, 7, 'Grace', 'QA', 39373, 'C2', 9, 'http://cv.example.com/7', TIMESTAMP '2023-06-12 13:47:48');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (21, 7, 'Grace', 'Designer', 43561, 'C1', NULL, 'http://cv.example.com/7', TIMESTAMP '2023-06-16 06:04:00');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (22, 5, 'Eve', 'QA', 58421, 'C2', 5, 'http://cv.example.com/5', TIMESTAMP '2023-08-08 08:35:18');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (23, 3, 'Charlie', 'Manager', 42883, 'C2', 5, 'http://cv.example.com/3', TIMESTAMP '2023-08-03 00:05:07');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (24, 7, 'Grace', 'Analyst', 35678, 'A1', 4, 'http://cv.example.com/7', TIMESTAMP '2023-02-16 11:33:00');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (25, 2, 'Bob', 'Designer', 33930, 'A2', 10, 'http://cv.example.com/2', TIMESTAMP '2023-08-19 22:39:37');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (26, 5, 'Eve', 'Manager', 38845, 'C1', 6, 'http://cv.example.com/5', TIMESTAMP '2023-12-20 10:15:43');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (27, 2, 'Bob', 'Manager', 64843, 'C1', 5, 'http://cv.example.com/2', TIMESTAMP '2021-02-23 22:57:40');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (28, 1, 'Alice', 'QA', 37908, 'C2', 5, 'http://cv.example.com/1', TIMESTAMP '2023-11-01 17:17:40');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (29, 8, 'Helen', 'Analyst', 37411, 'C1', NULL, 'http://cv.example.com/8', TIMESTAMP '2023-11-09 13:59:40');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (30, 8, 'Helen', 'Designer', 59040, 'C2', 7, 'http://cv.example.com/8', TIMESTAMP '2023-05-12 17:55:57');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (31, 10, 'Jane', 'Analyst', 41397, 'B2', 2, 'http://cv.example.com/10', TIMESTAMP '2023-10-01 15:32:06');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (32, 8, 'Helen', 'Designer', 59121, 'C1', 2, 'http://cv.example.com/8', TIMESTAMP '2023-12-29 14:07:14');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (33, 6, 'Frank', 'Analyst', 78223, 'B2', 2, 'http://cv.example.com/6', TIMESTAMP '2023-06-25 05:14:08');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (34, 7, 'Grace', 'Analyst', 70640, 'A2', 3, 'http://cv.example.com/7', TIMESTAMP '2023-02-24 20:09:42');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (35, 1, 'Alice', 'Manager', 47797, 'B2', NULL, 'http://cv.example.com/1', TIMESTAMP '2023-08-11 06:21:32');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (36, 1, 'Alice', 'Manager', 59724, 'C2', 8, 'http://cv.example.com/1', TIMESTAMP '2013-02-23 14:30:27');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (37, 9, 'Ian', 'QA', 82348, 'A1', 1, 'http://cv.example.com/9', TIMESTAMP '2023-08-06 11:17:28');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (38, 2, 'Bob', 'Manager', 42637, 'A2', 2, 'http://cv.example.com/2', TIMESTAMP '2023-12-25 21:23:16');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (39, 3, 'Charlie', 'Dev', 59320, 'C1', 2, 'http://cv.example.com/3', TIMESTAMP '2023-09-03 10:52:47');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (40, 9, 'Ian', 'Analyst', 45402, 'B1', 6, 'http://cv.example.com/9', TIMESTAMP '2023-12-26 20:22:17');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (41, 1, 'Alice', 'QA', 65791, 'B1', 3, 'http://cv.example.com/1', TIMESTAMP '2023-08-25 05:20:21');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (42, 10, 'Jane', 'Designer', 51636, 'C2', NULL, 'http://cv.example.com/10', TIMESTAMP '2023-01-30 08:51:08');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (43, 10, 'Jane', 'Designer', 55442, 'B2', 10, 'http://cv.example.com/10', TIMESTAMP '2023-12-01 01:48:46');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (44, 10, 'Jane', 'Analyst', 76824, 'C2', 10, 'http://cv.example.com/10', TIMESTAMP '2023-11-21 06:41:18');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (45, 9, 'Ian', 'Dev', 73335, 'B2', 1, 'http://cv.example.com/9', TIMESTAMP '2023-08-24 12:26:52');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (46, 2, 'Bob', 'Designer', 32437, 'C2', 3, 'http://cv.example.com/2', TIMESTAMP '2023-05-24 08:42:59');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (47, 1, 'Alice', 'Dev', 75264, 'B2', 5, 'http://cv.example.com/1', TIMESTAMP '2023-10-19 15:33:24');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (48, 6, 'Frank', 'Manager', 74211, 'A1', NULL, 'http://cv.example.com/6', TIMESTAMP '2023-05-15 19:53:20');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (49, 1, 'Alice', 'QA', 61618, 'C2', 6, 'http://cv.example.com/1', TIMESTAMP '2023-12-14 05:59:47');
INSERT INTO PersonChangesLog (Id, PersonId, Name, Position, Salary, EnglishLevel, ManagerId, CVLink, EditTimestamp) VALUES (50, 3, 'Charlie', 'QA', 78716, 'C2', 10, 'http://cv.example.com/3', TIMESTAMP '2023-03-04 20:18:05');

ANALYZE PersonChangesLog;

WITH history AS
(
    SELECT
        pcl.*,

        CASE pcl.EnglishLevel
            WHEN 'A1' THEN 1
            WHEN 'A2' THEN 2
            WHEN 'B1' THEN 3
            WHEN 'B2' THEN 4
            WHEN 'C1' THEN 5
            WHEN 'C2' THEN 6
        END AS current_level_rank,

        LAG(pcl.EnglishLevel) OVER
        (
            PARTITION BY pcl.PersonId
            ORDER BY pcl.EditTimestamp, pcl.Id
        ) AS previous_english_level,

        LAG(
            CASE pcl.EnglishLevel
                WHEN 'A1' THEN 1
                WHEN 'A2' THEN 2
                WHEN 'B1' THEN 3
                WHEN 'B2' THEN 4
                WHEN 'C1' THEN 5
                WHEN 'C2' THEN 6
            END
        ) OVER
        (
            PARTITION BY pcl.PersonId
            ORDER BY pcl.EditTimestamp, pcl.Id
        ) AS previous_level_rank

    FROM PersonChangesLog pcl
),
improvements AS
(
    SELECT *
    FROM history
    WHERE EditTimestamp >= TIMESTAMP '2023-01-01 00:00:00'
      AND EditTimestamp <  TIMESTAMP '2024-01-01 00:00:00'
      AND previous_level_rank IS NOT NULL
      AND current_level_rank > previous_level_rank
)
SELECT
    PersonId,
    Name,
    previous_english_level,
    EnglishLevel AS new_english_level,
    EditTimestamp
FROM improvements
ORDER BY PersonId, EditTimestamp;

WITH history AS
(
    SELECT
        pcl.*,

        CASE pcl.EnglishLevel
            WHEN 'A1' THEN 1
            WHEN 'A2' THEN 2
            WHEN 'B1' THEN 3
            WHEN 'B2' THEN 4
            WHEN 'C1' THEN 5
            WHEN 'C2' THEN 6
        END AS current_level_rank,

        LAG(
            CASE pcl.EnglishLevel
                WHEN 'A1' THEN 1
                WHEN 'A2' THEN 2
                WHEN 'B1' THEN 3
                WHEN 'B2' THEN 4
                WHEN 'C1' THEN 5
                WHEN 'C2' THEN 6
            END
        ) OVER
        (
            PARTITION BY pcl.PersonId
            ORDER BY pcl.EditTimestamp, pcl.Id
        ) AS previous_level_rank

    FROM PersonChangesLog pcl
)
SELECT DISTINCT
    PersonId,
    Name
FROM history
WHERE EditTimestamp >= TIMESTAMP '2023-01-01 00:00:00'
  AND EditTimestamp <  TIMESTAMP '2024-01-01 00:00:00'
  AND previous_level_rank IS NOT NULL
  AND current_level_rank > previous_level_rank
ORDER BY PersonId;

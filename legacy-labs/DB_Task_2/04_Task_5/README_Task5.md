# Task 5 — DB management and migration

Проєкт: **GameDevStudioDB** (PostgreSQL 17, база `game_dev_studio_db`). Це продовження Task 1–4; старі лабораторні **повторно не запускати**.

## Що реалізовано

| Пункт завдання | Реалізація | Де демонструвати |
|---|---|---|
| 1. Users, roles, access | `task5_reader` (SELECT), `task5_task_editor` (обмежений UPDATE), `task5_assignee1` (читання лише призначених задач через VIEW) | VS Code + pgAdmin 4 |
| 2. Backup/DR та recovery | PostgreSQL `pg_dump -Fc` → `.backup`; `pg_restore` у **нові, окремі бази**, перевірка після контрольованого видалення даних лише у копії | CMD/термінал + pgAdmin 4 |
| 3. Міграція до іншої СУБД | PostgreSQL → **SQLite**: CSV + JSON-маніфест → файл `.db`, таблиці, дані, PK, FK, прості та унікальні індекси, основні CHECK | VS Code + термінал + SQLite |

## Правила безпеки

1. **Не запускай** `00_create_database.sql` і `01_schema_and_seed.sql` знову — вони пересоздають схему.
2. `16_3_simulate_data_loss_in_copy.sql` запускай **тільки** в `game_dev_studio_task5_restored`. Перший `DO` перевіряє правильну назву бази.
3. `16_2_restore_copy.cmd` / `16_4_restore_recovered_copy.cmd` створюють **нові** бази. Якщо вже існують — зупиняться; **не використовують DROP DATABASE**.
4. Резервні копії та CSV містять реальні дані: зберігай локально, не публікуй.
5. **Не записуй пароль** PostgreSQL у код: програма запитає його; `pg_dump` запитає через `-W`.
6. Команди `SET ROLE` / `RESET ROLE` запускати в **одній вкладці Query Tool**. Для демонстрації помилки з читанням `employees` `RESET ROLE` запускається наступною командою в цій же вкладці.

## Пункт 1. Ролі та доступ — у pgAdmin 4

Відкрий `Databases → game_dev_studio_db → Query Tool` від адміністратора (`postgres`).

Виконай файли в порядку:

1. `01_Roles_And_Access/15_1_setup_roles_and_view.sql` — створює 3 NOLOGIN ролі та VIEW, **не змінюючи реальних даних**.
2. `15_2_check_privileges.sql` — матриця дозволів TRUE/FALSE.
3. `15_3_reader_allowed.sql` — `SET ROLE task5_reader`, читання `studios` працює.
4. `15_4_reader_denied.sql` — окремо спробуй читання `employees`: має бути **permission denied**.
5. `15_5_reset_role.sql` — у **тій самій сесії** поверни права адміністратора.
6. `15_6_editor_permissions.sql` — `task_status`/`estimated_hours` можна UPDATE, `title` — ні.
7. `15_7_assignee_view.sql` — бачимо тільки задачі `assigned_employee_id = 1`.
8. За бажанням `15_8_test_editor_transaction.sql` — зміна статусу під обмеженою роллю й фінальний ROLLBACK; таблиця не зміниться.

**Чому view замість RLS?** VIEW надає рольову **фільтрацію рядків без зміни налаштувань доступу до робочої таблиці**. `task5_assignee1` отримує `SELECT` тільки на view, не на всю `tasks`. Прямий доступ до таблиці для цієї ролі заборонений (за умови, що ніхто не надав його через інші ролі/PUBLIC). Це демонстрація row-level read restriction; в продакшені можливе використання PostgreSQL RLS.

**Реальні LOGIN-користувачі:** створені ролі — групи прав (`NOLOGIN`). За бажанням за документом `15_9_optional_create_login_README.txt` створи у pgAdmin Login/Group Role з власним паролем і надай йому `task5_reader`. Паролі у файлах проєкту не зберігаються.

## Пункт 2. Резервна копія та відновлення

Файли `02_Backup_Recovery/*.cmd` запускаються у **Windows CMD або PowerShell**. Краще запускати з терміналу VS Code, щоб бачити вивід. Якщо термінал — PowerShell, зайди у папку `02_Backup_Recovery` і виконуй:

```powershell
.\16_1_backup_postgres.cmd
.\16_2_restore_copy.cmd
# після демонстрації втрати даних у відновленій копії:
.\16_4_restore_recovered_copy.cmd
```
 У файлах встановлено стандартний шлях `C:\Program Files\PostgreSQL\17\bin`; якщо інший — зміни `PG_BIN`. Під час виконання введи пароль PostgreSQL.

1. Запусти `16_1_backup_postgres.cmd`. Результат: `02_Backup_Recovery/backups/task5_full.backup`. Це PostgreSQL **custom-format** (`pg_dump -F c`), не `.bacpac` (він для SQL Server).
2. Запусти `16_2_restore_copy.cmd`. Отримаєш **нову базу** `game_dev_studio_task5_restored`; початкова база `game_dev_studio_db` недоторкана.
3. У pgAdmin відкрий обидві бази та виконай `16_5_verify_original.sql` / `16_6_verify_restored_or_recovered.sql`. Зафіксуй однакові кількості `studios`, `employees`, `tasks`, `game_projects`.
4. У **`game_dev_studio_task5_restored`** окремо запусти `16_3_simulate_data_loss_in_copy.sql`. Лише в копії видалиться **одна задача**, тому `task_count` стане меншим на 1. Запит захищений перевіркою назви БД.
5. Запусти `16_4_restore_recovered_copy.cmd`: **ще одна нова база** `game_dev_studio_task5_recovered` з того самого `.backup`.
6. У pgAdmin виконай `16_6_verify_restored_or_recovered.sql` у `game_dev_studio_task5_recovered`: кількість задач повернулася до початкової. Для наочності покажи COUNT у трьох базах: original = N, damaged copy = N−1, recovered copy = N.

**Чому це Disaster Recovery?** Відновлення бази з незалежного фізичного файлу backup після модельованої втрати запису. **Ролі PostgreSQL глобальні для кластера** й у `pg_dump` однієї бази не входять: демонстраційні ролі створює окремий SQL `15_1`. Для глобальних ролей у реальній системі використовують `pg_dumpall --globals-only`.

Якщо тестові бази вже існують, `createdb` повідомить про конфлікт. **Не видаляй бази випадково**: за потреби залиш їх як доказ виконаної роботи або сам обери інші назви в `.cmd`.

## Пункт 3. Міграція PostgreSQL → SQLite

Це **справжня міграція в іншу СУБД**, а не експорт у файл `.sql`. Усі `public` таблиці, які реально є в робочій PostgreSQL базі на момент експорту, включно з Task 2–4, зчитуються з системного каталогу.

### Один раз підготувати Python

У терміналі VS Code:

```powershell
cd "04_Task_5\03_Migration_SQLite"
python --version
python -m pip install -r requirements.txt
```

Залежність лише для експорту PostgreSQL: `psycopg[binary]`. Модулі для SQLite вже входять у Python.

### Виконати 4 команди по черзі

```powershell
python 17_export_postgres.py
python 18_import_sqlite.py
python 19_verify_sqlite.py
python 20_sqlite_queries.py
```

Якщо підключення не стандартне, в поточному терміналі налаштуй середовище перед експортом:

```powershell
$env:PGHOST = "localhost"
$env:PGPORT = "5432"
$env:PGUSER = "postgres"
$env:PGDATABASE = "game_dev_studio_db"
```

`17_export_postgres.py` запитає пароль в терміналі, **не збереже його**. Експорт використовує `REPEATABLE READ READ ONLY`: усі таблиці зчитуються з **одного узгодженого snapshot**, без модифікацій даних.

### Що утвориться

```text
03_Migration_SQLite/
  requirements.txt
  migration_common.py
  17_export_postgres.py
  18_import_sqlite.py
  19_verify_sqlite.py
  20_sqlite_queries.py
  20_sqlite_queries.sql
  output/
    postgres_export/
      manifest.json               # таблиці, колонки, PK, FK, індекси, COUNT, SHA256
      studios.csv
      employees.csv
      tasks.csv
      ... CSV для КОЖНОЇ public-таблиці
    game_dev_studio_sqlite.db      # справжня БД SQLite
    sqlite_schema.sql             # DDL, що згенерований для SQLite
    migration_report.txt
```

`18_import_sqlite.py` **не перезаписує існуючу** `.db`. Якщо потрібно імпортувати повторно після нового експорту, збережи стару `.db` і лише тоді самостійно видали/перейменуй вихідний файл.

`19_verify_sqlite.py` перевіряє: **COUNT кожної таблиці, SHA-256 вмісту кожного рядка кожної таблиці (включно з NULL), кількість FK, наявність індексів, `PRAGMA foreign_key_check`, `PRAGMA integrity_check`**. Якщо щось не збігається — процес повертає помилку. Контрольні суми порівнюються з **миттєвим знімком на момент експорту**, а не з подальшими змінами PostgreSQL.

**Індекси:** PK, FK, прості btree й унікальні btree індекси переносяться. Непереносимі expression/partial/іншого типу індекси вказуються як пропущені в `migration_report.txt` і в маніфесті, їх треба переносити вручну. Основні CHECK із нашого проєкту реалізовано в SQLite в еквівалентному вигляді.

**Чесні технічні відмінності:** SQLite не має PostgreSQL ролей, PL/pgSQL функцій та `FOR EACH STATEMENT` тригерів. PostgreSQL-специфічну логіку необхідно адаптувати окремо. Для `tasks` скрипт створює **SQLite-аналоги row-trigger audit** (`INSERT`, `UPDATE`, `DELETE`) після завантаження даних. `transaction_id` у них умовно `0`, тому що SQLite не має `txid_current()`. `NUMERIC/DECIMAL` копіюється в TEXT **без втрати десяткових розрядів**; для числової арифметики в SQLite використовуй `CAST(... AS NUMERIC)` з урахуванням його моделі чисел.

### Що показувати викладачу

- **VS Code:** `15_1` (CREATE ROLE / GRANT / VIEW), `16_1` (pg_dump), `16_2/16_4` (pg_restore), `17`/`18`/`19` (migration).
- **pgAdmin 4:** перевірки ролей, три COUNT-и для оригіналу/пошкодженої копії/відновленої копії.
- **SQLite:** відкрий `.db` через будь-який SQLite viewer у VS Code або виконай `python 20_sqlite_queries.py`. Покажи JOIN `game_projects` ↔ `studios` ↔ `employees`, а також `PRAGMA foreign_key_list('tasks')`, `PRAGMA index_list('tasks')`.
- **Термінал:** `python 19_verify_sqlite.py` із фінальним `MIGRATION VERIFIED` — це найбільш переконлива перевірка цілісності даних.

## Чого лабораторна **не** робить автоматично

- Не запускає PostgreSQL користувача або сервер на твоєму ПК без твоєї участі.
- Не створює backup чи SQLite DB у ZIP наперед: вони формуються **з твоєї живої БД**, тому що актуальних даних локального PostgreSQL немає в архіві SQL-коду.
- Не переносить між різними СУБД PostgreSQL-специфічні процедури, логіку statement-trigger і ролі як готові об'єкти SQLite.

**Теза для захисту:** «Я налаштував обмежені права ролей і фільтрацію рядків через VIEW, створив custom backup PostgreSQL, відновив дані в новій базі після тестової втрати, а також мігрував усі таблиці до SQLite із перевіркою даних, первинних/зовнішніх ключів та індексів».

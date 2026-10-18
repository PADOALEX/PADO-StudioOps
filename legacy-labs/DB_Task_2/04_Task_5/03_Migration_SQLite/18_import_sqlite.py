"""Task 5: restore exported PostgreSQL CSV + structure into a NEW SQLite DB.

Run: python 18_import_sqlite.py
No external Python package required. Refuses to overwrite existing target.
"""
import csv
import json
from pathlib import Path
import sqlite3
from migration_common import (BINARY_TYPES, FLOAT_TYPES, INTEGER_TYPES,
    BOOLEAN_TYPES, build_create_table, ident, supported_indexes,
    unescape_csv_value)

ROOT = Path(__file__).resolve().parent
EXPORT_DIR = ROOT / 'output' / 'postgres_export'
SQLITE_FILE = ROOT / 'output' / 'game_dev_studio_sqlite.db'
SCHEMA_FILE = ROOT / 'output' / 'sqlite_schema.sql'
REPORT_FILE = ROOT / 'output' / 'migration_report.txt'


def convert(value, udt_name):
    if value is None:
        return None
    if udt_name in BINARY_TYPES:
        import base64
        return base64.b64decode(value)
    if udt_name in INTEGER_TYPES | BOOLEAN_TYPES:
        return int(value)
    if udt_name in FLOAT_TYPES:
        return float(value)
    return value


def load_csv(path, table, conn):
    with path.open(newline='', encoding='utf-8') as file:
        reader = csv.reader(file)
        header = next(reader)
        expected = [col['name'] for col in table['columns']]
        if header != expected:
            raise ValueError(f'Column order mismatch in {path}: {header} != {expected}')
        sql = f'INSERT INTO {ident(table["name"])} (' + ', '.join(map(ident, header)) + \
              ') VALUES (' + ', '.join('?' for _ in header) + ')'
        batch = []
        count = 0
        for fields in reader:
            if len(fields) != len(expected):
                raise ValueError(f'Invalid CSV field count in {path}: {fields}')
            batch.append(tuple(convert(unescape_csv_value(v), col['udt_name'])
                               for v, col in zip(fields, table['columns'])))
            if len(batch) >= 500:
                conn.executemany(sql, batch)
                count += len(batch)
                batch.clear()
        if batch:
            conn.executemany(sql, batch)
            count += len(batch)
    if count != table['row_count']:
        raise ValueError(f'{table["name"]}: CSV rows {count} != manifest {table["row_count"]}')
    return count


def create_sqlite_audit_triggers(conn, name_set, ddl_statements):
    # SQLite supports row triggers, but not PostgreSQL's FOR EACH STATEMENT.
    required = {'tasks', 'task_row_audit'}
    if not required.issubset(name_set):
        return
    definitions = [
        '''CREATE TRIGGER task5_sqlite_tasks_insert
        AFTER INSERT ON "tasks" BEGIN
          INSERT INTO "task_row_audit"
          ("action_name","task_id","task_title","old_status","new_status",
           "old_estimated_hours","new_estimated_hours","transaction_id")
          VALUES ('INSERT',NEW.task_id,NEW.title,NULL,NEW.task_status,
                  NULL,NEW.estimated_hours,0);
        END;''',
        '''CREATE TRIGGER task5_sqlite_tasks_update
        AFTER UPDATE ON "tasks" BEGIN
          INSERT INTO "task_row_audit"
          ("action_name","task_id","task_title","old_status","new_status",
           "old_estimated_hours","new_estimated_hours","transaction_id")
          VALUES ('UPDATE',NEW.task_id,NEW.title,OLD.task_status,NEW.task_status,
                  OLD.estimated_hours,NEW.estimated_hours,0);
        END;''',
        '''CREATE TRIGGER task5_sqlite_tasks_delete
        AFTER DELETE ON "tasks" BEGIN
          INSERT INTO "task_row_audit"
          ("action_name","task_id","task_title","old_status","new_status",
           "old_estimated_hours","new_estimated_hours","transaction_id")
          VALUES ('DELETE',OLD.task_id,OLD.title,OLD.task_status,NULL,
                  OLD.estimated_hours,NULL,0);
        END;'''
    ]
    for ddl in definitions:
        conn.execute(ddl)
        ddl_statements.append(ddl)


def main():
    manifest_file = EXPORT_DIR / 'manifest.json'
    if not manifest_file.exists():
        raise SystemExit('No export found. Run 17_export_postgres.py first.')
    if SQLITE_FILE.exists():
        raise SystemExit(f'Target exists: {SQLITE_FILE}. Move/delete it yourself before re-importing.')
    manifest = json.loads(manifest_file.read_text(encoding='utf-8'))
    tables = manifest['tables']
    names = {table['name'] for table in tables}
    if not tables:
        raise RuntimeError('Manifest has no tables.')
    ddl_statements = []
    notes = []
    SQLITE_FILE.parent.mkdir(parents=True, exist_ok=True)
    conn = sqlite3.connect(SQLITE_FILE)
    try:
        # FK references may point to tables created later; validate AFTER import.
        conn.execute('PRAGMA foreign_keys = OFF')
        conn.execute('BEGIN')
        for table in tables:
            table['all_table_names'] = names
            ddl = build_create_table(table)
            conn.execute(ddl)
            ddl_statements.append(ddl)
        for table in tables:
            count = load_csv(EXPORT_DIR / f'{table["name"]}.csv', table, conn)
            print(f'IMPORTED {table["name"]:<30} {count:>7} rows')
        for table in tables:
            for index, ddl, warning in supported_indexes(table):
                if ddl:
                    conn.execute(ddl)
                    ddl_statements.append(ddl)
                else:
                    notes.append(f'SKIPPED INDEX {table["name"]}.{index["name"]}: {warning}')
        create_sqlite_audit_triggers(conn, names, ddl_statements)
        conn.commit()
        conn.execute('PRAGMA foreign_keys = ON')
        violations = conn.execute('PRAGMA foreign_key_check').fetchall()
        if violations:
            raise RuntimeError(f'Foreign key violations after import: {violations[:10]}')
        problems = conn.execute('PRAGMA integrity_check').fetchone()[0]
        if problems != 'ok':
            raise RuntimeError(f'SQLite integrity_check: {problems}')
        SCHEMA_FILE.write_text('\n\n'.join(ddl_statements) + '\n', encoding='utf-8')
        report = (f'SOURCE: {manifest["source_db"]} (PostgreSQL)\n'
                  f'TARGET: {SQLITE_FILE.name} (SQLite)\n'
                  f'TABLES: {len(tables)}\n'
                  f'TOTAL ROWS: {sum(t["row_count"] for t in tables)}\n'
                  'PRAGMA foreign_key_check: 0 violations\n'
                  'PRAGMA integrity_check: ok\n'
                  'Numeric/DECIMAL values preserved as TEXT to avoid loss of cents.\n'
                  'PostgreSQL roles, stored procedures and STATEMENT triggers are not portable.\n'
                  'SQLite equivalents of Task 4 ROW audit triggers are created.\n'
                  + ('\n'.join(notes) if notes else 'No unsupported indexes detected.') + '\n')
        REPORT_FILE.write_text(report, encoding='utf-8')
        print('\n' + report)
    except Exception:
        conn.rollback()
        conn.close()
        # Do not leave a broken target that looks like a successful import.
        if SQLITE_FILE.exists():
            SQLITE_FILE.unlink()
        raise
    finally:
        try:
            conn.close()
        except Exception:
            pass


if __name__ == '__main__':
    main()

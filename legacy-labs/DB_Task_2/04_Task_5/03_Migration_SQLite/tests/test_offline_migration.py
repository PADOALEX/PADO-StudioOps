"""Offline smoke test; synthetic fixtures, not user's live PostgreSQL database."""
import csv
import hashlib
import json
from pathlib import Path
import sqlite3
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import migration_common as common
import importlib
import_mod = importlib.import_module('18_import_sqlite')
verify_mod = importlib.import_module('19_verify_sqlite')


def table(name, columns, pk, fks=None, indexes=None, rows=None):
    columns = [dict(name=n, udt_name=t, nullable=nullable, default=None) for n,t,nullable in columns]
    rows = rows or []
    digest = common.sha256_for_records(rows)
    return dict(name=name, columns=columns, primary_key=pk,
                foreign_keys=fks or [], indexes=indexes or [],
                postgres_check_constraints=[], row_count=len(rows),
                sha256=digest), rows

class TestTask5Migration(unittest.TestCase):
    def test_export_style_csv_to_sqlite_and_verify(self):
        with tempfile.TemporaryDirectory() as temp:
            base = Path(temp)
            export = base / 'postgres_export'
            export.mkdir()
            fk = dict(name='fk_tasks_employee', referenced_table='employees',
                      on_delete='SET NULL', on_update='NO ACTION',
                      columns=['assigned_employee_id'], referenced_columns=['employee_id'])
            employee_idx = dict(name='employees_email_key', is_unique=True,
                                is_primary=False, is_partial=False, is_expression=False,
                                method='btree', columns=['email'])
            task_idx = dict(name='ix_tasks_assigned_employee_id', is_unique=False,
                            is_primary=False, is_partial=False, is_expression=False,
                            method='btree', columns=['assigned_employee_id'])
            fixture = [
                table('employees', [('employee_id','int4',False),('email','varchar',False),
                                    ('salary','numeric',False)], ['employee_id'],
                      indexes=[employee_idx], rows=[['1','a@b','925.00'],['2','c@d','950.00']]),
                table('tasks', [('task_id','int4',False),('assigned_employee_id','int4',True),
                                ('title','varchar',False),('task_status','varchar',False),
                                ('estimated_hours','numeric',False),('priority','varchar',False)],
                      ['task_id'], fks=[fk], indexes=[task_idx],
                      rows=[['1001','1','First task','To Do','5.00','High'],
                            ['1002',None,'Second task','Done','2.50','Medium']]),
                table('task_row_audit', [('audit_id','int8',False),('action_name','varchar',False),
                                         ('task_id','int4',False),('task_title','varchar',False),
                                         ('old_status','varchar',True),('new_status','varchar',True),
                                         ('old_estimated_hours','numeric',True),('new_estimated_hours','numeric',True),
                                         ('transaction_id','int8',False)], ['audit_id'], rows=[])
            ]
            tables = []
            for metadata, records in fixture:
                tables.append(metadata)
                with (export / f'{metadata["name"]}.csv').open('w', newline='', encoding='utf-8') as f:
                    writer = csv.writer(f)
                    writer.writerow([col['name'] for col in metadata['columns']])
                    for record in records:
                        writer.writerow([common.escape_csv_value(v) for v in record])
            (export / 'manifest.json').write_text(json.dumps(dict(source_db='unit_test_pg', tables=tables)), encoding='utf-8')

            previous_import = [import_mod.EXPORT_DIR, import_mod.SQLITE_FILE, import_mod.SCHEMA_FILE, import_mod.REPORT_FILE]
            previous_verify = [verify_mod.manifest, verify_mod.dbfile]
            db = base / 'converted.db'
            try:
                import_mod.EXPORT_DIR = export
                import_mod.SQLITE_FILE = db
                import_mod.SCHEMA_FILE = base / 'generated.sql'
                import_mod.REPORT_FILE = base / 'report.txt'
                import_mod.main()
                verify_mod.manifest = export / 'manifest.json'
                verify_mod.dbfile = db
                verify_mod.main()
                with sqlite3.connect(db) as conn:
                    self.assertEqual(conn.execute('SELECT salary FROM employees WHERE employee_id=1').fetchone()[0], '925.00')
                    self.assertEqual(conn.execute('PRAGMA foreign_key_check').fetchall(), [])
                    self.assertEqual(conn.execute("SELECT count(*) FROM sqlite_master WHERE type='index' AND name='employees_email_key'").fetchone()[0], 1)
                    conn.execute('PRAGMA foreign_keys=ON')
                    with self.assertRaises(sqlite3.IntegrityError):
                        conn.execute('INSERT INTO tasks(task_id,assigned_employee_id,title,task_status,estimated_hours,priority) VALUES(1003,999,\'invalid\',\'To Do\',\'1.00\',\'Low\')')
                    conn.rollback()
                    conn.execute("UPDATE tasks SET task_status='In Progress' WHERE task_id=1001")
                    self.assertEqual(conn.execute("SELECT COUNT(*) FROM task_row_audit WHERE action_name='UPDATE'").fetchone()[0], 1)
            finally:
                import_mod.EXPORT_DIR, import_mod.SQLITE_FILE, import_mod.SCHEMA_FILE, import_mod.REPORT_FILE = previous_import
                verify_mod.manifest, verify_mod.dbfile = previous_verify

if __name__ == '__main__':
    unittest.main()

"""Run useful SQLite queries from the Python terminal, without extensions.
Run: python 20_sqlite_queries.py
"""
from pathlib import Path
import sqlite3

DB = Path(__file__).resolve().parent / 'output' / 'game_dev_studio_sqlite.db'
if not DB.exists():
    raise SystemExit('No SQLite DB yet. Run 17 and 18 first.')
with sqlite3.connect(f'{DB.as_uri()}?mode=ro', uri=True) as conn:
    for name in ('studios', 'employees', 'game_projects', 'tasks',
                 'project_members', 'project_technologies'):
        print(name, ':', conn.execute(f'SELECT COUNT(*) FROM "{name}"').fetchone()[0])
    print('\nJOIN (project + studio + lead):')
    for row in conn.execute('''
        SELECT gp.project_id, gp.title, s.name, e.first_name || ' ' || e.last_name
        FROM game_projects gp JOIN studios s ON gp.studio_id = s.studio_id
        LEFT JOIN employees e ON gp.project_lead_id = e.employee_id
        ORDER BY gp.project_id LIMIT 5
    '''):
        print(row)
    print('\nFK violations:', conn.execute('PRAGMA foreign_key_check').fetchall())
    print('Integrity:', conn.execute('PRAGMA integrity_check').fetchone()[0])
    print('task FKs:', conn.execute('PRAGMA foreign_key_list("tasks")').fetchall())
    print('task indexes:', conn.execute('PRAGMA index_list("tasks")').fetchall())

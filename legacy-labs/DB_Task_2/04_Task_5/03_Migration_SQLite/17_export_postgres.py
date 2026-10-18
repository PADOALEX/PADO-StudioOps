"""Task 5: export ALL public PostgreSQL base tables + metadata to CSV/JSON.

Run: python 17_export_postgres.py
Dependency: pip install -r requirements.txt
Exports a REPEATABLE READ, READ ONLY snapshot. Does not modify PostgreSQL.
"""
import csv
from datetime import datetime, timezone
import getpass
import json
import os
from pathlib import Path

from migration_common import digest_update, escape_csv_value, normalize

try:
    import psycopg
except ImportError as exc:
    raise SystemExit('Missing psycopg. Run: python -m pip install -r requirements.txt') from exc

ROOT = Path(__file__).resolve().parent
EXPORT_DIR = ROOT / 'output' / 'postgres_export'
MANIFEST = EXPORT_DIR / 'manifest.json'


SQL_TABLES = """
SELECT cls.relname
FROM pg_class AS cls
JOIN pg_namespace AS ns ON ns.oid = cls.relnamespace
WHERE ns.nspname = 'public' AND cls.relkind = 'r'
ORDER BY cls.relname
"""
SQL_COLUMNS = """
SELECT column_name, (is_nullable = 'YES') AS nullable, udt_name,
       column_default
FROM information_schema.columns
WHERE table_schema = 'public' AND table_name = %s
ORDER BY ordinal_position
"""
SQL_PRIMARY_KEY = """
SELECT a.attname
FROM pg_constraint AS con
JOIN pg_class AS tbl ON tbl.oid = con.conrelid
JOIN pg_namespace AS ns ON ns.oid = tbl.relnamespace
JOIN unnest(con.conkey) WITH ORDINALITY AS key(attnum, position) ON TRUE
JOIN pg_attribute AS a ON a.attrelid = tbl.oid AND a.attnum = key.attnum
WHERE ns.nspname = 'public' AND tbl.relname = %s AND con.contype = 'p'
ORDER BY key.position
"""
SQL_FK = """
SELECT con.conname, parent.relname AS referenced_table, con.confdeltype, con.confupdtype,
       ARRAY(SELECT a.attname FROM unnest(con.conkey) WITH ORDINALITY AS x(attnum, pos)
             JOIN pg_attribute a ON a.attrelid = child.oid AND a.attnum = x.attnum
             ORDER BY x.pos) AS columns,
       ARRAY(SELECT a.attname FROM unnest(con.confkey) WITH ORDINALITY AS x(attnum, pos)
             JOIN pg_attribute a ON a.attrelid = parent.oid AND a.attnum = x.attnum
             ORDER BY x.pos) AS referenced_columns
FROM pg_constraint con
JOIN pg_class child ON child.oid = con.conrelid
JOIN pg_namespace ns ON ns.oid = child.relnamespace
JOIN pg_class parent ON parent.oid = con.confrelid
WHERE ns.nspname = 'public' AND child.relname = %s AND con.contype = 'f'
ORDER BY con.conname
"""
SQL_INDEXES = """
SELECT idx.relname, ix.indisunique, ix.indisprimary,
       (ix.indpred IS NOT NULL), (ix.indexprs IS NOT NULL), am.amname,
       ARRAY(SELECT pg_get_indexdef(ix.indexrelid, p.n, TRUE)
             FROM generate_series(1, ix.indnkeyatts) AS p(n)) AS key_columns
FROM pg_index ix
JOIN pg_class tbl ON tbl.oid = ix.indrelid
JOIN pg_namespace ns ON ns.oid = tbl.relnamespace
JOIN pg_class idx ON idx.oid = ix.indexrelid
JOIN pg_am am ON am.oid = idx.relam
WHERE ns.nspname = 'public' AND tbl.relname = %s
ORDER BY idx.relname
"""
SQL_CHECKS = """
SELECT con.conname, pg_get_constraintdef(con.oid)
FROM pg_constraint con
JOIN pg_class tbl ON tbl.oid = con.conrelid
JOIN pg_namespace ns ON ns.oid = tbl.relnamespace
WHERE ns.nspname = 'public' AND tbl.relname = %s AND con.contype = 'c'
ORDER BY con.conname
"""

FK_ACTION = {'a': 'NO ACTION', 'r': 'RESTRICT', 'c': 'CASCADE',
             'n': 'SET NULL', 'd': 'SET DEFAULT'}


def export():
    import hashlib
    host = os.environ.get('PGHOST', 'localhost')
    port = int(os.environ.get('PGPORT', '5432'))
    user = os.environ.get('PGUSER', 'postgres')
    dbname = os.environ.get('PGDATABASE', 'game_dev_studio_db')
    password = os.environ.get('PGPASSWORD')
    if password is None:
        password = getpass.getpass(f'PostgreSQL password for {user} (not saved): ')
    EXPORT_DIR.mkdir(parents=True, exist_ok=True)
    conn = psycopg.connect(host=host, port=port, user=user, password=password,
                           dbname=dbname, autocommit=True)
    try:
        with conn.cursor() as cur:
            cur.execute('BEGIN TRANSACTION ISOLATION LEVEL REPEATABLE READ READ ONLY')
            cur.execute(SQL_TABLES)
            names = [x[0] for x in cur.fetchall()]
            if not names:
                raise RuntimeError('No public tables found. Check database name/privileges.')
            tables = []
            for name in names:
                cur.execute(SQL_COLUMNS, (name,))
                columns = [dict(name=r[0], nullable=r[1], udt_name=r[2], default=r[3])
                           for r in cur.fetchall()]
                cur.execute(SQL_PRIMARY_KEY, (name,))
                pk = [r[0] for r in cur.fetchall()]
                cur.execute(SQL_FK, (name,))
                fks = [dict(name=r[0], referenced_table=r[1],
                            on_delete=FK_ACTION[r[2]], on_update=FK_ACTION[r[3]],
                            columns=r[4], referenced_columns=r[5]) for r in cur.fetchall()]
                cur.execute(SQL_INDEXES, (name,))
                indexes = [dict(name=r[0], is_unique=r[1], is_primary=r[2],
                                is_partial=r[3], is_expression=r[4], method=r[5],
                                columns=r[6]) for r in cur.fetchall()]
                cur.execute(SQL_CHECKS, (name,))
                checks = [dict(name=r[0], definition=r[1]) for r in cur.fetchall()]
                if not pk:
                    raise RuntimeError(f'Table {name} does not have a primary key: cannot verify deterministic copy.')
                table = dict(name=name, columns=columns, primary_key=pk,
                             foreign_keys=fks, indexes=indexes,
                             postgres_check_constraints=checks)
                tables.append(table)

            for table in tables:
                name = table['name']
                ordered_fields = ', '.join(psycopg.sql.Identifier(c['name']).as_string(conn)
                                           for c in table['columns'])
                order_by = ', '.join(psycopg.sql.Identifier(key).as_string(conn)
                                     for key in table['primary_key'])
                query = (f'SELECT {ordered_fields} FROM '
                         f'{psycopg.sql.Identifier("public", name).as_string(conn)} ORDER BY {order_by}')
                cur.execute(query)
                h = hashlib.sha256()
                total = 0
                destination = EXPORT_DIR / f'{name}.csv'
                with destination.open('w', newline='', encoding='utf-8') as f:
                    writer = csv.writer(f)
                    writer.writerow([col['name'] for col in table['columns']])
                    for raw in cur:
                        values = [normalize(v, col['udt_name'])
                                  for v, col in zip(raw, table['columns'])]
                        digest_update(h, values)
                        writer.writerow([escape_csv_value(v) for v in values])
                        total += 1
                table['row_count'] = total
                table['sha256'] = h.hexdigest()
                print(f'EXPORTED {name:<30} {total:>7} rows, indexes={len(table["indexes"])}')
            manifest = dict(source_db=dbname, source_dbms='PostgreSQL',
                            target_dbms='SQLite', schema='public',
                            exported_at_utc=datetime.now(timezone.utc).isoformat(),
                            tables=tables)
            MANIFEST.write_text(json.dumps(manifest, indent=2, ensure_ascii=False), encoding='utf-8')
            print(f'\nExported {len(tables)} tables from one consistent snapshot -> {EXPORT_DIR}')
            cur.execute('ROLLBACK')
    finally:
        conn.close()


if __name__ == '__main__':
    export()

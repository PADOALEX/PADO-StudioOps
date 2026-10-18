"""Task 5 verification: compare per-table content checksums to PG snapshot.

Run: python 19_verify_sqlite.py
No psycopg/PG connection is needed; verification uses the immutable export manifest.
"""
import hashlib
import json
from pathlib import Path
import sqlite3
from migration_common import BINARY_TYPES, digest_update, ident, normalize, supported_indexes

ROOT = Path(__file__).resolve().parent
manifest = ROOT / 'output' / 'postgres_export' / 'manifest.json'
dbfile = ROOT / 'output' / 'game_dev_studio_sqlite.db'


def main():
    if not manifest.exists() or not dbfile.exists():
        raise SystemExit('Missing output. Run 17_export_postgres.py and 18_import_sqlite.py first.')
    metadata = json.loads(manifest.read_text(encoding='utf-8'))
    connection = sqlite3.connect(f'{dbfile.as_uri()}?mode=ro', uri=True)
    failed = []
    total = 0
    try:
        for table in metadata['tables']:
            cols = table['columns']
            names = ', '.join(ident(c['name']) for c in cols)
            order = ', '.join(ident(key) for key in table['primary_key'])
            rows = connection.execute(f'SELECT {names} FROM {ident(table["name"])} ORDER BY {order}')
            digest = hashlib.sha256()
            count = 0
            for row in rows:
                values = [normalize(v, col['udt_name'])
                          for v, col in zip(row, cols)]
                digest_update(digest, values)
                count += 1
            total += count
            ok = count == table['row_count'] and digest.hexdigest() == table['sha256']
            state = 'OK' if ok else 'FAIL'
            print(f'{state:4} {table["name"]:<29} PG={table["row_count"]:>6} SQLite={count:>6} SHA256 match={digest.hexdigest() == table["sha256"]}')
            if not ok:
                failed.append(f'Rows/data mismatch: {table["name"]}')
            fk_count = len(table['foreign_keys'])
            sqlite_fk = connection.execute(f'PRAGMA foreign_key_list({ident(table["name"])})').fetchall()
            fk_groups = {row[0] for row in sqlite_fk}
            if len(fk_groups) != fk_count:
                failed.append(f'FK count mismatch: {table["name"]} PG={fk_count}, SQLite={len(fk_groups)}')
            idx_names = {idx[1] for idx in connection.execute(f'PRAGMA index_list({ident(table["name"])})')}
            for index, ddl, note in supported_indexes(table):
                if ddl and index['name'] not in idx_names:
                    failed.append(f'Missing index: {table["name"]}.{index["name"]}')
        fk_violations = connection.execute('PRAGMA foreign_key_check').fetchall()
        integrity = connection.execute('PRAGMA integrity_check').fetchone()[0]
        if fk_violations:
            failed.append(f'FK violations: {fk_violations[:5]}')
        if integrity != 'ok':
            failed.append(f'SQLite integrity check: {integrity}')
        print(f'\nTABLES: {len(metadata["tables"])} | ROWS: {total} | Foreign key violations: {len(fk_violations)} | Integrity: {integrity}')
        if failed:
            for error in failed:
                print('FAILED:', error)
            raise SystemExit(1)
        print('MIGRATION VERIFIED: row counts + field data hashes + FKs + indexes + integrity.')
    finally:
        connection.close()


if __name__ == '__main__':
    main()

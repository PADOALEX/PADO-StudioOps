"""Shared lossless field conversion and SQLite schema helpers for Task 5."""
import base64
import hashlib
import json
import re

INTEGER_TYPES = {'int2', 'int4', 'int8', 'oid'}
NUMERIC_TYPES = {'numeric', 'money'}
FLOAT_TYPES = {'float4', 'float8'}
BINARY_TYPES = {'bytea'}
BOOLEAN_TYPES = {'bool'}


def ident(name):
    return '"' + name.replace('"', '""') + '"'


def sqlite_type(udt_name):
    if udt_name in INTEGER_TYPES | BOOLEAN_TYPES:
        return 'INTEGER'
    if udt_name in NUMERIC_TYPES:
        return 'TEXT'  # Lossless Decimal representation (e.g. 925.00).
    if udt_name in FLOAT_TYPES:
        return 'REAL'
    if udt_name in BINARY_TYPES:
        return 'BLOB'
    return 'TEXT'


def normalize(value, udt_name):
    if value is None:
        return None
    if udt_name in BINARY_TYPES:
        return base64.b64encode(bytes(value)).decode('ascii')
    if udt_name in BOOLEAN_TYPES:
        return '1' if value else '0'
    if udt_name in INTEGER_TYPES:
        return str(int(value))
    if udt_name in FLOAT_TYPES:
        return repr(float(value))
    if udt_name in {'json', 'jsonb'} or udt_name.startswith('_'):
        if isinstance(value, (dict, list, tuple)):
            return json.dumps(value, ensure_ascii=False, sort_keys=True, default=str)
    if hasattr(value, 'isoformat') and udt_name in {
        'date', 'timestamp', 'timestamptz', 'time', 'timetz'
    }:
        return value.isoformat(sep=' ') if udt_name in {'timestamp', 'timestamptz'} else value.isoformat()
    return str(value)


def escape_csv_value(value):
    # \\N denotes SQL NULL. Backslash-prefixed values are escaped with another slash.
    if value is None:
        return r'\N'
    return '\\' + value if value.startswith('\\') else value


def unescape_csv_value(value):
    if value == r'\N':
        return None
    return value[1:] if value.startswith('\\') else value


def digest_update(hasher, record):
    hasher.update(json.dumps(record, ensure_ascii=False, separators=(',', ':')).encode('utf-8'))
    hasher.update(b'\n')


def sha256_for_records(records):
    h = hashlib.sha256()
    for record in records:
        digest_update(h, record)
    return h.hexdigest()


# Portable constraint equivalents for the explicit constraints of Task 1-4.
# Other PostgreSQL CHECK expressions are kept in manifest as metadata and need manual porting.
PORTABLE_CHECKS = {
    'studios': ['"founded_year" BETWEEN 1970 AND 2026'],
    'employees': ['CAST("salary" AS REAL) >= 0'],
    'game_projects': [
        'CAST("budget" AS REAL) >= 0',
        '"project_status" IN (\'Concept\',\'Pre-production\',\'Production\',\'Testing\',\'Released\',\'Cancelled\')'
    ],
    'milestones': ['"milestone_status" IN (\'Planned\',\'In Progress\',\'Completed\',\'Delayed\')'],
    'tasks': [
        '"priority" IN (\'Low\',\'Medium\',\'High\',\'Critical\')',
        '"task_status" IN (\'To Do\',\'In Progress\',\'Code Review\',\'Testing\',\'Done\')',
        'CAST("estimated_hours" AS REAL) > 0'
    ],
    'personchangeslog': ['"englishlevel" IN (\'A1\',\'A2\',\'B1\',\'B2\',\'C1\',\'C2\')'],
    'task_row_audit': ['"action_name" IN (\'INSERT\',\'UPDATE\',\'DELETE\')'],
    'task_statement_audit': [
        '"action_name" IN (\'UPDATE\',\'DELETE\')',
        '"affected_rows" >= 0'
    ],
}


def build_create_table(table):
    table_name = table['name']
    pk = table['primary_key']
    colnames = {col['name'] for col in table['columns']}
    parts = []
    for col in table['columns']:
        name = col['name']
        coltype = sqlite_type(col['udt_name'])
        if pk == [name] and coltype == 'INTEGER':
            parts.append(f'{ident(name)} INTEGER PRIMARY KEY')
        else:
            definition = f'{ident(name)} {coltype}'
            if not col['nullable']:
                definition += ' NOT NULL'
            if col['udt_name'] in {'timestamp', 'timestamptz'} and name in {'build_date', 'calculated_at', 'changed_at'}:
                definition += ' DEFAULT CURRENT_TIMESTAMP'
            if col['udt_name'] in BOOLEAN_TYPES and col.get('default') is not None:
                default_lower = col['default'].lower()
                if default_lower in ('true', 'false'):
                    definition += ' DEFAULT ' + ('1' if default_lower == 'true' else '0')
            parts.append(definition)
    if len(pk) > 1 or (len(pk) == 1 and sqlite_type(next(c['udt_name'] for c in table['columns'] if c['name'] == pk[0])) != 'INTEGER'):
        parts.append('PRIMARY KEY (' + ', '.join(ident(s) for s in pk) + ')')
    for fk in table['foreign_keys']:
        if fk['referenced_table'] not in table['all_table_names']:
            raise ValueError(f'{table_name}: foreign key targets omitted table {fk["referenced_table"]}')
        fields = ', '.join(ident(s) for s in fk['columns'])
        otherfields = ', '.join(ident(s) for s in fk['referenced_columns'])
        parts.append(f'FOREIGN KEY ({fields}) REFERENCES {ident(fk["referenced_table"])} ({otherfields}) '
                     f'ON DELETE {fk["on_delete"]} ON UPDATE {fk["on_update"]}')
    for check in PORTABLE_CHECKS.get(table_name, []):
        parts.append('CHECK (' + check + ')')
    return f'CREATE TABLE {ident(table_name)} (\n  ' + ',\n  '.join(parts) + '\n);'


def supported_indexes(table):
    names = {c['name'] for c in table['columns']}
    for index in table['indexes']:
        if index['is_primary']:
            continue
        if index.get('is_partial') or index.get('is_expression') or index.get('method') not in ('btree', None):
            yield index, None, 'Partial/expression/non-btree index requires manual port'
            continue
        cols = []
        for expr in index['columns']:
            normalized_name = expr.strip('"')
            if normalized_name not in names or not re.fullmatch(r'"?[A-Za-z_][A-Za-z0-9_]*"?', expr):
                cols = []
                break
            cols.append(normalized_name)
        if not cols:
            yield index, None, 'Unsupported index expression'
            continue
        ddl = ('CREATE UNIQUE INDEX' if index['is_unique'] else 'CREATE INDEX')
        ddl += f' {ident(index["name"])} ON {ident(table["name"])} ('
        ddl += ', '.join(ident(col) for col in cols) + ');'
        yield index, ddl, None

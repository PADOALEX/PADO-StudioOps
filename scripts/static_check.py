"""Lightweight offline structural checks; NOT a substitute for dotnet build or PG tests."""
from pathlib import Path
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[1]
project = root / 'src/PADO.StudioOps.Api'

for path in [*root.rglob('*.csproj'), *root.rglob('*.slnx')]:
    ET.parse(path)
    print('XML OK:', path.relative_to(root))

js = project / 'wwwroot/js/board.js'
subprocess.run(['node', '--check', str(js)], check=True)
print('JS syntax OK')

migration = (project / 'Data/Migrations/202610180001_InitialStudioOps.cs').read_text()
expected = ['studios', 'employees', 'employee_profiles', 'game_projects', 'milestones', 'tasks', 'technologies', 'game_builds', 'project_members', 'project_technologies', 'app_users']
for name in expected:
    assert re.search(r'CREATE TABLE ' + name + r'\s*\(', migration), name
print('Migration contains all 11 tables')
for file in project.rglob('*.cs'):
    text = file.read_text()
    assert 'DROP SCHEMA' not in text, file
assert 'localStorage.' not in js.read_text()
assert (root / 'legacy-labs/DB_Task_2/00_Setup/01_schema_and_seed.sql').exists()
print('Legacy scripts are isolated; application scripts do not destroy public schema')

print('Static checks passed (SDK/DB execution still required).')

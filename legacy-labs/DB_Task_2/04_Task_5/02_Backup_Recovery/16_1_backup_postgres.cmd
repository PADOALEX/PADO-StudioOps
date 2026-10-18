@echo off
setlocal
set "PG_BIN=C:\Program Files\PostgreSQL\17\bin"
set "PGHOST=localhost"
set "PGPORT=5432"
set "PGUSER=postgres"
set "SOURCE_DB=game_dev_studio_db"
set "BACKUP=%~dp0backups\task5_full.backup"
if not exist "%PG_BIN%\pg_dump.exe" (
  echo ERROR: pg_dump.exe not found. Edit PG_BIN in this .cmd file.
  exit /b 1
)
if not exist "%~dp0backups" mkdir "%~dp0backups"
echo Backing up %SOURCE_DB% to %BACKUP% (schema, data, indexes, constraints, triggers)...
"%PG_BIN%\pg_dump.exe" -h "%PGHOST%" -p "%PGPORT%" -U "%PGUSER%" -W -F c -b -v --file "%BACKUP%" "%SOURCE_DB%"
if errorlevel 1 (
  echo BACKUP FAILED.
  exit /b 1
)
echo BACKUP SUCCESS: %BACKUP%
"%PG_BIN%\pg_restore.exe" --list "%BACKUP%" | findstr /C:"TABLE DATA" /C:"INDEX" /C:"FK CONSTRAINT"
endlocal

@echo off
setlocal
set "PG_BIN=C:\Program Files\PostgreSQL\17\bin"
set "PGHOST=localhost"
set "PGPORT=5432"
set "PGUSER=postgres"
set "TARGET_DB=game_dev_studio_task5_recovered"
set "BACKUP=%~dp0backups\task5_full.backup"
if not exist "%BACKUP%" (echo ERROR: Create the backup first. & exit /b 1)
if not exist "%PG_BIN%\createdb.exe" (echo ERROR: Change PG_BIN path. & exit /b 1)
echo Creating SECOND safe restored copy: %TARGET_DB%
"%PG_BIN%\createdb.exe" -h "%PGHOST%" -p "%PGPORT%" -U "%PGUSER%" -W -T template0 "%TARGET_DB%"
if errorlevel 1 (echo ERROR: Target database may already exist; nothing was overwritten. & exit /b 1)
"%PG_BIN%\pg_restore.exe" -h "%PGHOST%" -p "%PGPORT%" -U "%PGUSER%" -W --no-owner --no-acl --exit-on-error -d "%TARGET_DB%" "%BACKUP%"
if errorlevel 1 (echo RESTORE FAILED. Original database untouched. & exit /b 1)
echo DATA RECOVERY SUCCESS. Compare tasks COUNT in original, damaged, recovered databases.
endlocal

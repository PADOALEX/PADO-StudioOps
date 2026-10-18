@echo off
setlocal
set "PG_BIN=C:\Program Files\PostgreSQL\17\bin"
set "PGHOST=localhost"
set "PGPORT=5432"
set "PGUSER=postgres"
set "TARGET_DB=game_dev_studio_task5_restored"
set "BACKUP=%~dp0backups\task5_full.backup"
if not exist "%BACKUP%" (echo ERROR: Create a backup first with 16_1_backup_postgres.cmd & exit /b 1)
if not exist "%PG_BIN%\createdb.exe" (echo ERROR: Change PG_BIN path. & exit /b 1)
echo Creating NEW database %TARGET_DB%. Existing databases will NOT be replaced.
"%PG_BIN%\createdb.exe" -h "%PGHOST%" -p "%PGPORT%" -U "%PGUSER%" -W -T template0 "%TARGET_DB%"
if errorlevel 1 (echo ERROR: New database could not be created. Existing DB is untouched. & exit /b 1)
echo Restoring data from %BACKUP% into %TARGET_DB% ...
"%PG_BIN%\pg_restore.exe" -h "%PGHOST%" -p "%PGPORT%" -U "%PGUSER%" -W --no-owner --no-acl --exit-on-error -d "%TARGET_DB%" "%BACKUP%"
if errorlevel 1 (echo RESTORE FAILED. Check errors. The original DB was not touched. & exit /b 1)
echo RESTORE SUCCESS: %TARGET_DB%
echo Open pgAdmin, connect to %TARGET_DB%, and run 16_6_verify_restored_or_recovered.sql.
endlocal

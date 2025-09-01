@echo off

set MOD_NAME=BeyondStorage2
set SOURCE_DIR=bin\Debug

rem set OUT_DIR="D:\SteamLibrary\steamapps\common\7 Days To Die\Mods\%MOD_NAME%"
set OUT_DIR="%APPDATA%\7DaysToDie\Mods\%MOD_NAME%"

rem echo OUT_DIR is %OUT_DIR%
rem pause

echo Checking for source files...

if not exist %SOURCE_DIR%\%MOD_NAME%.dll (
    echo Error: %MOD_NAME%.dll not found in %SOURCE_DIR%
    echo Current directory: %CD%
    goto :error
)

if not exist %SOURCE_DIR%\%MOD_NAME%.pdb (
    echo Error: %MOD_NAME%.pdb not found in %SOURCE_DIR%
    echo Current directory: %CD%
    goto :error
)

echo Found both files, attempting to move...

move %SOURCE_DIR%\%MOD_NAME%.dll %OUT_DIR%
if errorlevel 1 (
    echo Error: Failed to move %MOD_NAME%.dll
    goto :error
)

move %SOURCE_DIR%\%MOD_NAME%.pdb %OUT_DIR%
if errorlevel 1 (
    echo Error: Failed to move %MOD_NAME%.pdb
    goto :error
)

goto :success

:error
echo Push failed
pause
goto :end

:success
echo Pushed successfully
rem uncomment the pause if required for debugging
rem pause

:end
rem just exit
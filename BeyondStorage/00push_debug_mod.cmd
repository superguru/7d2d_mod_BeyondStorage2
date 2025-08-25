@echo off

rem set OUT_DIR="D:\SteamLibrary\steamapps\common\7 Days To Die\Mods\BeyondStorage2"
set OUT_DIR="%APPDATA%\7DaysToDie\Mods\BeyondStorage2"

rem echo OUT_DIR is %OUT_DIR%
rem pause

echo Checking for source files...

if not exist BeyondStorage2.dll (
    echo Error: BeyondStorage2.dll not found in current directory
    echo Current directory: %CD%
    goto :error
)

if not exist BeyondStorage2.pdb (
    echo Error: BeyondStorage2.pdb not found in current directory
    echo Current directory: %CD%
    goto :error
)

echo Found both files, attempting to move...

move BeyondStorage2.dll %OUT_DIR%
if errorlevel 1 (
    echo Error: Failed to move BeyondStorage2.dll
    goto :error
)

move BeyondStorage2.pdb %OUT_DIR%
if errorlevel 1 (
    echo Error: Failed to move BeyondStorage2.pdb
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
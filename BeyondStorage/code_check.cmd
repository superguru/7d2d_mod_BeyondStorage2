@echo off

python code_check.py %*

if not "%errorlevel%"=="0" goto :error

echo Success! The last command completed without errors.
goto :end

:error
echo An error occurred! The exit code was: %errorlevel%

:end
rem pause
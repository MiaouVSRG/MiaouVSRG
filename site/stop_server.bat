@echo off
cd /d "%~dp0bin"

echo Stopping Apache...
httpd.exe -k stop

pause
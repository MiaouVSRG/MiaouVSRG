@echo off
cd /d "%~dp0bin"

echo Starting Apache...
httpd.exe -k start

pause
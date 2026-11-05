@echo off
setlocal
set "LUMEN_CONVERSATION_PROVIDER=hybrid"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\Start-LumenBackend.ps1"
pause

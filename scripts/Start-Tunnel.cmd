@echo off
setlocal
title ChatGPT MCP Tunnel
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Start-Tunnel.ps1" %*
set "exitcode=%ERRORLEVEL%"
if not "%exitcode%"=="0" echo Tunnel exited with code %exitcode%.
pause
exit /b %exitcode%

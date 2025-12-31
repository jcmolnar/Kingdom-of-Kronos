@echo off
echo Starting Accessory and Armor Migration...
PowerShell -NoProfile -ExecutionPolicy Bypass -File "%~dp0migrate_accessories.ps1"
pause

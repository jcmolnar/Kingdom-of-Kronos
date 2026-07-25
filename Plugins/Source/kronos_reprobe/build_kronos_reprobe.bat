@echo off
REM Build kronos_reprobe.dll - T1Vista RE runtime confirmation probe (READ-ONLY).
REM 32-bit REQUIRED (T1Vista is x86; source uses naked functions + inline asm).
call "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvarsall.bat" x86
if errorlevel 1 ( echo vcvarsall failed & exit /b 1 )
cd /d "%~dp0"
cl /nologo /LD /EHsc /O2 kronos_reprobe.cpp /Fe:kronos_reprobe.dll
if errorlevel 1 ( echo BUILD FAILED & exit /b 1 )
copy /y kronos_reprobe.dll ..\..\kronos_reprobe.dll >nul
echo BUILD OK: kronos_reprobe.dll (copied to Plugins\)

@echo off
REM Build kronos_virtualitems.dll - Phase C PROBE (read-only datablock-name probe).
REM 32-bit REQUIRED (T1Vista is x86; source uses naked/inline asm).
call "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvarsall.bat" x86
if errorlevel 1 ( echo vcvarsall failed & exit /b 1 )
cd /d "%~dp0"
cl /nologo /LD /EHsc /O2 kronos_virtualitems.cpp /Fe:kronos_virtualitems.dll
if errorlevel 1 ( echo BUILD FAILED & exit /b 1 )
copy /y kronos_virtualitems.dll ..\..\kronos_virtualitems.dll >nul
echo BUILD OK: kronos_virtualitems.dll (copied to Plugins\)

@echo off
REM Build kronos_datetime.dll - real calendar date/time console commands.
REM 32-bit REQUIRED (T1Vista is x86; source uses naked/inline asm).
call "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvarsall.bat" x86
if errorlevel 1 ( echo vcvarsall failed & exit /b 1 )
cd /d "%~dp0"
cl /nologo /LD /EHsc /O2 kronos_datetime.cpp /Fe:kronos_datetime.dll
if errorlevel 1 ( echo BUILD FAILED & exit /b 1 )
copy /y kronos_datetime.dll ..\..\kronos_datetime.dll >nul
echo BUILD OK: kronos_datetime.dll (copied to Plugins\)

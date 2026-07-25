@echo off
REM Build ForceExitPlugin.dll - forceExit() hard process exit console command.
REM 32-bit REQUIRED (T1Vista is x86; source uses naked/inline asm).
call "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvarsall.bat" x86 2>nul
if errorlevel 1 call "C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools\VC\Auxiliary\Build\vcvarsall.bat" x86
if errorlevel 1 ( echo vcvarsall failed & exit /b 1 )
cd /d "%~dp0"
cl /nologo /LD /EHsc /O2 ForceExitPlugin.cpp /Fe:ForceExitPlugin.dll
if errorlevel 1 ( echo BUILD FAILED & exit /b 1 )
copy /y ForceExitPlugin.dll ..\..\ForceExitPlugin.dll >nul
echo BUILD OK: ForceExitPlugin.dll (copied to Plugins\)

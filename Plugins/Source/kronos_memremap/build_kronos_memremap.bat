@echo off
REM Build kronos_memremap.dll -- PROTOTYPE mem.dll hook-relocation shim (32-bit).
call "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvarsall.bat" x86
if errorlevel 1 ( echo vcvarsall failed & exit /b 1 )
cd /d "%~dp0"
cl /nologo /LD /EHsc /O2 kronos_memremap.cpp /Fe:kronos_memremap.dll
if errorlevel 1 ( echo BUILD FAILED & exit /b 1 )
echo BUILD OK: kronos_memremap.dll

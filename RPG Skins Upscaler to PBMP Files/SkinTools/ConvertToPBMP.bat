@echo off
REM ============================================
REM Convert BMP to PBMP Format
REM Uses MakeSkin.exe to convert 8-bit BMP files to PBMP (Tribes format)
REM ============================================

setlocal enabledelayedexpansion

REM Set paths
set "SKINTOOLS_DIR=%~dp0"
set "PALETTE=pal3"

REM Check if input file was provided
if "%~1"=="" (
    echo.
    echo Usage: ConvertToPBMP.bat [input.bmp] [output.bmp]
    echo   Or drag and drop a BMP file onto this script
    echo.
    echo This script converts 8-bit BMP files to PBMP format (Tribes-optimized)
    echo using MakeSkin.exe with the pal3 palette.
    echo.
    echo If only one file is provided, output will be: [filename]_pbmp.bmp
    echo.
    pause
    exit /b 1
)

set "INPUT_FILE=%~1"
set "OUTPUT_FILE=%~2"

REM Check if input file exists
if not exist "%INPUT_FILE%" (
    echo ERROR: Input file not found: %INPUT_FILE%
    pause
    exit /b 1
)

REM If output file not specified, create one based on input filename
if "!OUTPUT_FILE!"=="" (
    set "INPUT_BASE=%~n1"
    set "INPUT_DIR=%~dp1"
    set "OUTPUT_FILE=%INPUT_DIR%%INPUT_BASE%_pbmp.bmp"
)

echo.
echo ============================================
echo Converting BMP to PBMP Format
echo ============================================
echo Input file:  %INPUT_FILE%
echo Output file: %OUTPUT_FILE%
echo Palette:     %PALETTE%
echo.
echo Note: MakeSkin.exe converts BMP to PBMP format (Tribes-optimized)
echo       Output files use .bmp extension but are in PBMP format internally
echo.

REM Change to SkinTools directory
cd /d "%SKINTOOLS_DIR%"

REM Check if MakeSkin.exe exists
if not exist "%SKINTOOLS_DIR%MakeSkin.exe" (
    echo ERROR: MakeSkin.exe not found in %SKINTOOLS_DIR%
    echo Please ensure MakeSkin.exe is in the same directory as this script.
    pause
    exit /b 1
)

REM Check if palette file exists
set "PALETTE_PAL=%SKINTOOLS_DIR%palettes\%PALETTE%.pal"
set "PALETTE_ACT=%SKINTOOLS_DIR%palettes\%PALETTE%PS.act"
if not exist "%PALETTE_PAL%" (
    if not exist "%PALETTE_ACT%" (
        echo WARNING: Palette file not found in palettes folder.
        echo MakeSkin will look for %PALETTE%.pal in current directory.
    )
)

REM Run MakeSkin.exe to convert to PBMP
echo Converting to PBMP format...
echo Command: MakeSkin.exe %PALETTE% "%INPUT_FILE%" "%OUTPUT_FILE%"
echo.

"%SKINTOOLS_DIR%MakeSkin.exe" %PALETTE% "%INPUT_FILE%" "%OUTPUT_FILE%"

if errorlevel 1 (
    echo.
    echo ERROR: MakeSkin.exe conversion failed!
    echo.
    echo Make sure:
    echo   - Input file is an 8-bit indexed BMP file
    echo   - Input file is 256x256 pixels (or will be resized)
    echo   - Palette file (%PALETTE%.pal) is available
    echo.
    pause
    exit /b 1
)

REM Verify output file was created
if not exist "%OUTPUT_FILE%" (
    echo.
    echo ERROR: Output file was not created: %OUTPUT_FILE%
    pause
    exit /b 1
)

REM Get file size for verification
for %%F in ("%OUTPUT_FILE%") do set "OUTPUT_SIZE=%%~zF"

echo.
echo ============================================
echo SUCCESS!
echo ============================================
echo File converted to PBMP format successfully!
echo.
echo Output file: %OUTPUT_FILE%
echo File size:   %OUTPUT_SIZE% bytes
echo.
echo Note: PBMP files use .bmp extension but contain PBMP format internally.
echo       This is the Tribes-optimized bitmap format.
echo.
pause





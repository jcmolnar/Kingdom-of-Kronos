@echo off
REM ============================================
REM Starsiege Tribes 1 Skin Automation Script
REM Uses Python scripts for palette conversion
REM ============================================

setlocal enabledelayedexpansion

REM Set paths
set "SKINTOOLS_DIR=%~dp0"
set "TARGET_DIR=C:\Dynamix\Tribes\RPG\Skins"
set "PALETTE=pal3"

REM Check if input file was provided
if "%~1"=="" (
    echo.
    echo Usage: MakeFedmonsterSkin.bat [input.bmp or input.png]
    echo   Or drag and drop a BMP or PNG file onto this script
    echo.
    pause
    exit /b 1
)

set "INPUT_FILE=%~1"
REM Extract prefix from input filename (name without extension)
set "PREFIX=%~n1"

REM Set up output directory structure: Conversions\[PREFIX]\
set "CONVERSIONS_DIR=%SKINTOOLS_DIR%Conversions"
set "OUTPUT_DIR=%CONVERSIONS_DIR%\%PREFIX%"
set "VOLUME_NAME=%OUTPUT_DIR%\%PREFIX%.vol"

REM Check if input file exists
if not exist "%INPUT_FILE%" (
    echo ERROR: Input file not found: %INPUT_FILE%
    pause
    exit /b 1
)

echo.
echo ============================================
echo Processing Skin: %PREFIX%
echo ============================================
echo Input file: %INPUT_FILE%
echo Palette: %PALETTE%
echo Prefix: %PREFIX%
echo.

REM Change to SkinTools directory
cd /d "%SKINTOOLS_DIR%"

REM Create output directory structure
if not exist "%OUTPUT_DIR%" (
    echo Creating output directory: %OUTPUT_DIR%
    mkdir "%OUTPUT_DIR%"
)

REM Convert image to 8-bit indexed color using Python
set "CONVERTED_FILE=%OUTPUT_DIR%\%PREFIX%_pal3.bmp"
set "USE_CONVERTED=0"
set "PALETTE_ACT=%SKINTOOLS_DIR%palettes\pal3PS.act"
set "PYTHON_SCRIPT=%SKINTOOLS_DIR%ConvertSkin.py"

REM Check for Python
set "PYTHON_CMD="
where python >nul 2>&1
if %errorlevel% equ 0 (
    set "PYTHON_CMD=python"
) else (
    where python3 >nul 2>&1
    if %errorlevel% equ 0 (
        set "PYTHON_CMD=python3"
    )
)

REM Convert image using Python script
if "!PYTHON_CMD!"=="" (
    echo ============================================
    echo ERROR: Python not found
    echo ============================================
    echo Python is required for this script.
    echo Please install Python 3 and ensure it's in your PATH.
    echo.
    echo After installing Python, run:
    echo   python -m pip install Pillow numpy
    echo.
    pause
    exit /b 1
)

if not exist "!PYTHON_SCRIPT!" (
    echo ERROR: Python script not found: !PYTHON_SCRIPT!
    pause
    exit /b 1
)

if not exist "!PALETTE_ACT!" (
    echo ERROR: Palette file not found: !PALETTE_ACT!
    pause
    exit /b 1
)

echo ============================================
echo Converting image to 8-bit indexed BMP...
echo ============================================
echo Using Python script for pixel-by-pixel palette remapping
echo - Resizing to 256x256 pixels
echo - Finding closest palette color for each pixel
echo - Converting RGB to palette indices (0-255)
echo - Creating 8-bit indexed BMP
echo.

echo Command: "!PYTHON_CMD!" "!PYTHON_SCRIPT!" "%INPUT_FILE%" "%CONVERTED_FILE%" "!PALETTE_ACT!"
"!PYTHON_CMD!" "!PYTHON_SCRIPT!" "%INPUT_FILE%" "%CONVERTED_FILE%" "!PALETTE_ACT!"

if errorlevel 1 (
    echo.
    echo ERROR: Python conversion failed.
    pause
    exit /b 1
)

if not exist "%CONVERTED_FILE%" (
    echo.
    echo ERROR: Converted file not created: %CONVERTED_FILE%
    pause
    exit /b 1
)

echo.
echo Successfully converted to 8-bit indexed BMP with pal3 palette!
set "INPUT_FILE=%CONVERTED_FILE%"
echo.

REM Verify palette file exists (MakeSkin looks for pal3.pal or pal3.act)
set "PALETTE_PAL=%SKINTOOLS_DIR%palettes\%PALETTE%.pal"
if exist "%PALETTE_PAL%" (
    echo Verified palette file: %PALETTE_PAL%
) else if exist "%PALETTE_ACT%" (
    echo Verified palette file: %PALETTE_ACT%
) else (
    echo WARNING: Palette file not found in palettes folder.
    echo MakeSkin will look for %PALETTE%.pal in current directory.
)

REM Process the skin
echo.
echo Processing skin with MakeSkin.exe...
echo Command format: MakeSkin.exe %PALETTE% [input.bmp] [output.bmp]
echo Note: MakeSkin.exe converts BMP to PBMP format (Tribes-optimized)

set "OUTPUT_FILE=%OUTPUT_DIR%\%PREFIX%.bmp"
echo.
echo Processing: %PREFIX%
echo   Input:  %INPUT_FILE%
echo   Output: !OUTPUT_FILE! (PBMP format)
echo   Command: MakeSkin.exe %PALETTE% "%INPUT_FILE%" "!OUTPUT_FILE!"

REM Run makeskin - this applies the pal3 palette and converts to PBMP format
"%SKINTOOLS_DIR%MakeSkin.exe" %PALETTE% "%INPUT_FILE%" "!OUTPUT_FILE!"

if errorlevel 1 (
    echo ERROR: MakeSkin.exe failed to process %PREFIX%
    pause
    exit /b 1
)

REM Verify output file was created
if not exist "!OUTPUT_FILE!" (
    echo ERROR: MakeSkin.exe did not create output file: !OUTPUT_FILE!
    pause
    exit /b 1
)

REM Add to volume
echo Adding to volume...
"%SKINTOOLS_DIR%VT.EXE" -sp "%VOLUME_NAME%" "!OUTPUT_FILE!" >nul 2>&1

REM Check if file was actually added (VT.EXE may return non-zero even on success)
REM Verify by checking if volume exists and has content
if not exist "%VOLUME_NAME%" (
    echo ERROR: Volume was not created. Failed to add %PREFIX% to volume.
    pause
    exit /b 1
)

echo Successfully processed and added %PREFIX% to volume.

REM Verify volume contents
echo.
echo ============================================
echo Verifying volume contents...
echo ============================================
"%SKINTOOLS_DIR%VT.EXE" "%VOLUME_NAME%"

REM Check if target directory exists
if not exist "%TARGET_DIR%" (
    echo.
    echo WARNING: Target directory does not exist: %TARGET_DIR%
    echo Creating directory...
    mkdir "%TARGET_DIR%"
)

REM Copy volume to target directory
echo.
echo ============================================
echo Copying volume to target directory...
echo ============================================
copy "%VOLUME_NAME%" "%TARGET_DIR%\" >nul

if errorlevel 1 (
    echo ERROR: Failed to copy volume to target directory
    pause
    exit /b 1
)

echo.
echo ============================================
echo SUCCESS!
echo ============================================
echo Volume created: %VOLUME_NAME%
echo Volume copied to: %TARGET_DIR%
echo.
echo All files saved to: %OUTPUT_DIR%
echo Your %PREFIX% skin is ready!
echo.

REM Clean up converted file if it was created
if !USE_CONVERTED! equ 1 (
    if exist "%CONVERTED_FILE%" (
        echo Cleaning up temporary converted file...
        del "%CONVERTED_FILE%"
    )
)

pause

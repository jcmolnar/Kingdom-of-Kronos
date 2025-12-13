@echo off
REM ============================================
REM Starsiege Tribes 1 Loading Screen Script
REM Uses Python scripts for palette conversion
REM Outputs at 640x480 resolution
REM ============================================

setlocal enabledelayedexpansion

REM Set paths
set "SKINTOOLS_DIR=%~dp0"
set "TARGET_DIR=C:\Dynamix\Tribes\RPG\Skins"
set "PALETTE=pal3"

REM Check if input file was provided
if "%~1"=="" (
    echo.
    echo Usage: MakeFedmonsterSkin - Loading Screen edit.bat [input.bmp or input.png]
    echo   Or drag and drop a BMP or PNG file onto this script
    echo.
    echo This script creates a loading screen at custom resolution.
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
set "VOLUME_NAME=%OUTPUT_DIR%\%PREFIX%LoadingScreen.vol"

REM Check if input file exists
if not exist "%INPUT_FILE%" (
    echo ERROR: Input file not found: %INPUT_FILE%
    pause
    exit /b 1
)

echo.
echo ============================================
echo Processing Loading Screen: %PREFIX%
echo ============================================
echo Input file: %INPUT_FILE%
echo.

REM Prompt for width
:prompt_width
set "LOADING_WIDTH="
set /p "LOADING_WIDTH=Enter output width (default: 640): "
if "!LOADING_WIDTH!"=="" set "LOADING_WIDTH=640"
REM Validate it's a number by trying to do arithmetic
set /a "TEST_WIDTH=!LOADING_WIDTH!" >nul 2>&1
if errorlevel 1 (
    echo ERROR: Width must be a number. Please try again.
    goto prompt_width
)
REM Check it's greater than 0
if !LOADING_WIDTH! LEQ 0 (
    echo ERROR: Width must be greater than 0. Please try again.
    goto prompt_width
)

REM Prompt for height
:prompt_height
set "LOADING_HEIGHT="
set /p "LOADING_HEIGHT=Enter output height (default: 480): "
if "!LOADING_HEIGHT!"=="" set "LOADING_HEIGHT=480"
REM Validate it's a number by trying to do arithmetic
set /a "TEST_HEIGHT=!LOADING_HEIGHT!" >nul 2>&1
if errorlevel 1 (
    echo ERROR: Height must be a number. Please try again.
    goto prompt_height
)
REM Check it's greater than 0
if !LOADING_HEIGHT! LEQ 0 (
    echo ERROR: Height must be greater than 0. Please try again.
    goto prompt_height
)

echo.
echo Output resolution: %LOADING_WIDTH%x%LOADING_HEIGHT%
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
set "PALETTE_ACT=%SKINTOOLS_DIR%palettes\pal3PS.act"
set "PYTHON_SCRIPT=%SKINTOOLS_DIR%ConvertLoadingScreen.py"

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
echo - Resizing to %LOADING_WIDTH%x%LOADING_HEIGHT% pixels
echo - Finding closest palette color for each pixel
echo - Converting RGB to palette indices (0-255)
echo - Creating 8-bit indexed BMP
echo.

echo Command: "!PYTHON_CMD!" "!PYTHON_SCRIPT!" "%INPUT_FILE%" "%CONVERTED_FILE%" "!PALETTE_ACT!" %LOADING_WIDTH% %LOADING_HEIGHT%
"!PYTHON_CMD!" "!PYTHON_SCRIPT!" "%INPUT_FILE%" "%CONVERTED_FILE%" "!PALETTE_ACT!" %LOADING_WIDTH% %LOADING_HEIGHT%

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

REM Remove old volume if it exists
if exist "%VOLUME_NAME%" (
    echo Removing old volume: %VOLUME_NAME%
    del "%VOLUME_NAME%"
)

REM Process loading screen with MakeSkin.exe
echo.
echo Processing loading screen with MakeSkin.exe...
echo Command format: MakeSkin.exe %PALETTE% [input.bmp] [output.bmp]
echo Note: MakeSkin.exe converts BMP to PBMP format (Tribes-optimized)
echo       Output files use .bmp extension but are in PBMP format internally
echo.

set "OUTPUT_FILE=%OUTPUT_DIR%\%PREFIX%.bmp"
echo.
echo Processing loading screen
echo   Input:  %INPUT_FILE%
echo   Output: !OUTPUT_FILE! (PBMP format, %LOADING_WIDTH%x%LOADING_HEIGHT%)
echo   Command: MakeSkin.exe %PALETTE% "%INPUT_FILE%" "!OUTPUT_FILE!"
echo.

REM Run makeskin - this applies the pal3 palette and converts to PBMP format
"%SKINTOOLS_DIR%MakeSkin.exe" %PALETTE% "%INPUT_FILE%" "!OUTPUT_FILE!"

if errorlevel 1 (
    echo ERROR: MakeSkin.exe failed to process loading screen
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
    echo ERROR: Volume was not created. Failed to add loading screen to volume.
    pause
    exit /b 1
)

echo Successfully processed and added loading screen to volume.

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
echo Your %PREFIX% loading screen is ready!
echo Resolution: %LOADING_WIDTH%x%LOADING_HEIGHT%
echo.

REM Clean up converted file if it was created
if exist "%CONVERTED_FILE%" (
    echo Cleaning up temporary converted file...
    del "%CONVERTED_FILE%"
)

pause

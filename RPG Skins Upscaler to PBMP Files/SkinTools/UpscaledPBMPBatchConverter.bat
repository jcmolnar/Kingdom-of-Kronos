@echo off
REM ============================================
REM Starsiege Tribes 1 Skin Batch Converter
REM Converts directory of images to PBMP and packs into separate VOLs
REM Grouping: [BaseName].larmor.png -> [BaseName].vol
REM ============================================

setlocal enabledelayedexpansion

REM Set paths
set "SKINTOOLS_DIR=%~dp0"
set "SOURCE_DIR=%SKINTOOLS_DIR%..\..\Upscaled Skins"
set "TARGET_DIR=C:\Dynamix\Tribes\RPG\Skins"
set "PALETTE=pal3"

REM Output configuration
set "CONVERSIONS_DIR=%SKINTOOLS_DIR%Conversions"
set "OUTPUT_DIR=C:\Users\Joe\Desktop\Kingdom of Kronos V0.8.1 Development\RPG Skins Upscaler to PBMP Files\SkinTools\UpscaledConvertedBMPs"

echo.
echo ============================================
echo   Upscaled Skins Batch Converter
echo ============================================
echo Source: %SOURCE_DIR%
echo Output: %OUTPUT_DIR%
echo Grouping: Individual VOLs per skin base name
echo Palette: %PALETTE%
echo ============================================
echo.

REM Change to SkinTools directory
cd /d "%SKINTOOLS_DIR%"

REM Check source directory
if not exist "%SOURCE_DIR%" (
    echo ERROR: Source directory not found: "%SOURCE_DIR%"
    echo Please make sure the 'Upscaled Skins' folder exists in the project root.
    pause
    exit /b 1
)

REM Create output directory structure
if not exist "%OUTPUT_DIR%" (
    echo Creating output directory: %OUTPUT_DIR%
    mkdir "%OUTPUT_DIR%"
)

REM Clean up old VOL files in output directory to ensure fresh builds
REM echo Cleaning old VOL files...
REM if exist "%OUTPUT_DIR%\*.vol" del "%OUTPUT_DIR%\*.vol"

REM Check dependencies
set "PYTHON_CMD="
where python >nul 2>&1
if %errorlevel% equ 0 set "PYTHON_CMD=python"
if "%PYTHON_CMD%"=="" (
    where python3 >nul 2>&1
    if %errorlevel% equ 0 set "PYTHON_CMD=python3"
)

if "%PYTHON_CMD%"=="" (
    echo ERROR: Python not found.
    pause
    exit /b 1
)

set "PYTHON_SCRIPT=%SKINTOOLS_DIR%ConvertSkin.py"
set "PALETTE_ACT=%SKINTOOLS_DIR%palettes\pal3PS.act"

if not exist "!PYTHON_SCRIPT!" (
    echo ERROR: Missing ConvertSkin.py
    pause
    exit /b 1
)

REM ---------------------------------------------------------
REM MAIN LOOP: Process all PNG files in source directory
REM ---------------------------------------------------------

for %%F in ("%SOURCE_DIR%\*.png") do (
    set "FILENAME=%%~nF"
    set "INPUT_FILE=%%F"
    set "TEMP_INDEXED=%OUTPUT_DIR%\!FILENAME!_indexed.bmp"
    set "FINAL_PBMP=%OUTPUT_DIR%\!FILENAME!.bmp"
    
    REM Extract Base Name (Token 1 before first dot)
    REM e.g. robegreen.larmor -> robegreen
    for /f "tokens=1 delims=." %%A in ("!FILENAME!") do set "BASENAME=%%A"
    
    REM set "CURRENT_VOL=%OUTPUT_DIR%\!BASENAME!.vol"
    
    echo.
    echo ------------------------------------------
    echo Processing: !FILENAME!
    echo Target Vol: !BASENAME!.vol
    echo ------------------------------------------
    
    REM 1. Convert to 8-bit indexed BMP via Python
    echo [1/3] Converting to 8-bit indexed BMP...
    "!PYTHON_CMD!" "!PYTHON_SCRIPT!" "!INPUT_FILE!" "!TEMP_INDEXED!" "!PALETTE_ACT!"
    
    if errorlevel 1 (
        echo ERROR: Python conversion failed for !FILENAME!
    ) else (
        
        REM 2. Convert to PBMP using MakeSkin
        echo [2/3] Converting to PBMP...
        "%SKINTOOLS_DIR%MakeSkin.exe" %PALETTE% "!TEMP_INDEXED!" "!FINAL_PBMP!"
        
        if errorlevel 1 (
            echo ERROR: MakeSkin failed for !FILENAME!
        ) else (
            
            REM 3. Add to SPECIFIC Volume
            echo [3/3] Adding to !BASENAME!.vol... (SKIPPED)
            REM "%SKINTOOLS_DIR%VT.EXE" -sp "!CURRENT_VOL!" "!FINAL_PBMP!" >nul 2>&1
            
            REM Cleanup
        )
        
        REM Cleanup temp indexed file
        if exist "!TEMP_INDEXED!" del "!TEMP_INDEXED!"
    )
)

REM ---------------------------------------------------------
REM FINISH
REM ---------------------------------------------------------

echo.
echo ============================================
echo BATCH PROCESSING COMPLETE
echo ============================================

REM Copy all generated volumes to target dir
REM if exist "%TARGET_DIR%" (
    REM echo.
    REM echo Copying all .vol files to: %TARGET_DIR%
    REM copy "%OUTPUT_DIR%\*.vol" "%TARGET_DIR%\" >nul
    REM if not errorlevel 1 echo Copy Success.
REM )

echo.
echo All conversions saved to: %OUTPUT_DIR%
echo.
pause

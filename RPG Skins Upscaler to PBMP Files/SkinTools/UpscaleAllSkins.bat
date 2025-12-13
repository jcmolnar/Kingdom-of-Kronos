@echo off
setlocal enabledelayedexpansion

:: CONFIGURATION
set "WAIFU2X_DIR=waifu2x-ncnn-vulkan-20250915-windows"
set "BASE_INPUT_DIR=..\..\Original Skin Extracts"
set "BASE_OUTPUT_DIR=..\..\Upscaled Skins"

echo ========================================================
echo Kingdom of Kronos - Local Waifu2x Autoscaler
echo ========================================================
echo.

:: Check for waifu2x executable
if not exist "%WAIFU2X_DIR%\waifu2x-ncnn-vulkan.exe" (
    echo ERROR: waifu2x executable not found at:
    echo %WAIFU2X_DIR%\waifu2x-ncnn-vulkan.exe
    echo.
    pause
    exit /b
)

echo Found waifu2x! Starting batch processing...
echo.

:: Process Job 1: Player Skins
call :PROCESS_FOLDER "Player Skins PNG" "Player Skins Upscaled"

:: Process Job 2: RPG Model Textures
call :PROCESS_FOLDER "RPG Model Textures PNG" "RPG Model Textures Upscaled"

echo.
echo ========================================================
echo Done! All batch jobs completed.
echo ========================================================
pause
exit /b

:: ----------------------------------------------------------
:: SUBROUTINE: Process a specific folder
:: Arguments: %1 = Input Subfolder Name, %2 = Output Folder Name
:: ----------------------------------------------------------
:PROCESS_FOLDER
set "SUB_INPUT=%~1"
set "SUB_OUTPUT=%~2"
set "FULL_INPUT=%BASE_INPUT_DIR%\%SUB_INPUT%"
set "FULL_OUTPUT=%BASE_OUTPUT_DIR%\%SUB_OUTPUT%"

echo --------------------------------------------------------
echo Processing: %SUB_INPUT%
echo Output to:  %SUB_OUTPUT%
echo --------------------------------------------------------

if not exist "%FULL_INPUT%" (
    echo [WARNING] Input folder not found: %FULL_INPUT%
    goto :EOF
)

:: Ensure output directory exists
if not exist "%FULL_OUTPUT%" mkdir "%FULL_OUTPUT%"

:: Loop through PNGs
set "count=0"
for %%F in ("%FULL_INPUT%\*.png") do (
    set "FILENAME=%%~nF"
    set "TARGET_FILE=%FULL_OUTPUT%\%%~nF.png"
    
    echo   [Upscaling] %%~nxF...
    
    :: Run waifu2x (-s 4 = 4x scale, -n 1 = Denoise Lvl 1, -x = TTA Mode, -m = CU-Net Model)
    "%WAIFU2X_DIR%\waifu2x-ncnn-vulkan.exe" -i "%%F" -o "!TARGET_FILE!" -s 4 -n 1 -x -m models-cunet
    
    if exist "!TARGET_FILE!" (
        set /a count+=1
    ) else (
        echo   [ERROR] Failed to process %%~nxF
    )
)
echo   Processed !count! images in %SUB_INPUT%.
echo.
goto :EOF

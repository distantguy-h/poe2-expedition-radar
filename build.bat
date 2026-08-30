@echo off
echo ========================================
echo  PoE2 Expedition Scanner - Build Script
echo ========================================
echo.

echo [1/2] Building...
dotnet publish PoE2ExpeditionScanner.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:IncludeAllContentForSelfExtract=true -o "%~dp0publish"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo BUILD FAILED!
    pause
    exit /b 1
)

echo.
echo [2/2] Done!
echo Output: %~dp0publish\PoE2ExpeditionScanner.exe
echo.
pause

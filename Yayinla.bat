@echo off
chcp 65001 > nul
title NotKosem - EXE Yayinlama

echo ==========================================
echo          NOTKOSEM - EXE YAYINLAMA
echo ==========================================
echo.

dotnet publish NotKosem.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true

if errorlevel 1 (
    echo.
    echo Yayinlama sirasinda hata olustu.
    pause
    exit /b 1
)

echo.
echo EXE hazir:
echo bin\Release\net8.0-windows\win-x64\publish\NotKosem.exe
echo.
pause

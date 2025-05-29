@echo off
echo Building SketchBlade as self-contained application...

REM Очистка предыдущих сборок
if exist "bin\Release\net9.0-windows\win-x64\publish" (
    echo Cleaning previous build...
    rmdir /s /q "bin\Release\net9.0-windows\win-x64\publish"
)

REM Сборка self-contained приложения
echo Building self-contained application...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true

if %ERRORLEVEL% neq 0 (
    echo Build failed!
    pause
    exit /b 1
)

REM Создание папки для распространения
set DIST_DIR=dist
if exist "%DIST_DIR%" (
    echo Cleaning distribution directory...
    rmdir /s /q "%DIST_DIR%"
)
mkdir "%DIST_DIR%"

REM Копирование исполняемого файла
echo Copying executable...
copy "bin\Release\net9.0-windows\win-x64\publish\SketchBlade.exe" "%DIST_DIR%\"

REM Копирование папки Resources
echo Copying Resources folder...
xcopy "Resources" "%DIST_DIR%\Resources\" /E /I /Y

REM Создание README для пользователя
echo Creating README...
(
echo SketchBlade - Self-Contained Application
echo =======================================
echo.
echo Это self-contained приложение, которое не требует установки .NET Runtime.
echo.
echo Структура файлов:
echo - SketchBlade.exe - основной исполняемый файл
echo - Resources/ - папка с ресурсами игры ^(изображения, локализации, сохранения^)
echo.
echo ВАЖНО: Папка Resources должна находиться в той же директории, что и SketchBlade.exe
echo.
echo Для запуска просто запустите SketchBlade.exe
echo.
echo Системные требования:
echo - Windows 10/11 ^(x64^)
echo - Минимум 100 MB свободного места
) > "%DIST_DIR%\README.txt"

echo.
echo Build completed successfully!
echo.
echo Distribution files are in the '%DIST_DIR%' folder:
dir "%DIST_DIR%" /B
echo.
echo You can now distribute the contents of the '%DIST_DIR%' folder.
echo.
pause 
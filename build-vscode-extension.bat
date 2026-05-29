@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "EXTENSION_DIR=%SCRIPT_DIR%vscode-extension"
set "ARTIFACTS_DIR=%SCRIPT_DIR%artifacts"

if not exist "%EXTENSION_DIR%\package.json" (
    echo Could not find vscode-extension\package.json
    exit /b 1
)

pushd "%EXTENSION_DIR%" || exit /b 1

if not exist "node_modules" (
    if exist "package-lock.json" (
        call npm ci
    ) else (
        call npm install
    )

    if errorlevel 1 (
        popd
        exit /b 1
    )
)

call npm run compile
if errorlevel 1 (
    popd
    exit /b 1
)

for /f "usebackq delims=" %%I in (`powershell -NoProfile -Command "(Get-Content package.json -Raw | ConvertFrom-Json).name"`) do set "EXTENSION_NAME=%%I"
for /f "usebackq delims=" %%I in (`powershell -NoProfile -Command "(Get-Content package.json -Raw | ConvertFrom-Json).version"`) do set "EXTENSION_VERSION=%%I"

if not exist "%ARTIFACTS_DIR%" mkdir "%ARTIFACTS_DIR%"

set "VSIX_PATH=%ARTIFACTS_DIR%\%EXTENSION_NAME%-%EXTENSION_VERSION%.vsix"

call npm run package -- --out "%VSIX_PATH%"
set "EXIT_CODE=%ERRORLEVEL%"

popd
exit /b %EXIT_CODE%
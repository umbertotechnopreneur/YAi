@echo off
setlocal

pushd "%~dp0" >nul || exit /b 1

where code >nul 2>&1
if errorlevel 1 (
	echo VS Code command-line launcher was not found on PATH.
	echo Install the "code" command in PATH, then run this file again.
	popd >nul
	exit /b 1
)

start "" code .

popd >nul
exit /b 0
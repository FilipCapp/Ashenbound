@echo off
cd /d "%~dp0"
set "GODOT=C:\Users\capul\Desktop\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe"
if not exist "%GODOT%" (
  echo Missing Godot executable: %GODOT%
  pause
  exit /b 1
)
"%GODOT%" --path "%~dp0."
if errorlevel 1 (
  echo Godot exited with an error.
  pause
)

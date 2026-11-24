@echo off
setlocal
cd /d "%~dp0"
if not exist Build mkdir Build
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /optimize+ /out:Build\Ashenbound.exe /reference:System.Drawing.dll /reference:System.Windows.Forms.dll src\Core.cs src\Game.cs src\AudioEngine.cs src\PixelArt.cs src\Render.cs src\App.cs src\Tests.cs
exit /b %errorlevel%

@echo off
REM Genera el .exe distribuible de Renombrador (Windows 64-bit).
REM Un solo archivo con todo incluido: tus clientes no instalan nada.
REM Uso: doble clic, o desde la carpeta Renombrador ejecuta  publicar.bat

dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

echo.
echo Listo. Tu programa para vender esta en:
echo   bin\Release\net10.0\win-x64\publish\Renombrador.exe
pause

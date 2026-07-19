#!/bin/bash
# Genera el .exe distribuible de Renombrador (Windows 64-bit).
# El resultado es UN solo archivo con todo incluido: tus clientes no instalan nada.
# Uso (desde Git Bash, en la carpeta Renombrador):  bash publicar.sh

set -e
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

echo ""
echo "Listo. Tu programa para vender esta en:"
echo "  bin/Release/net10.0/win-x64/publish/Renombrador.exe"

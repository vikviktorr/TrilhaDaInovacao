#!/usr/bin/env bash
# Instalador Windows: dist/TrilhaDaInovacao-Setup-<versão>.exe (NSIS).
# Usa o makensis do sistema ou, sem ele, uma cópia local extraída do
# repositório do Fedora em Terceiros/nsis (não precisa de sudo).
set -euo pipefail
cd "$(dirname "$0")/.."

VERSAO=$(grep -oP 'Versao = "\K[^"]+' Assets/TrilhaDaInovacao/Scripts/Editor/ConfiguradorXR.cs)
[ -f Builds/Windows/TrilhaDaInovacao.exe ] || { echo "Rode antes: Ferramentas/build.sh windows"; exit 1; }

if command -v makensis >/dev/null; then
  MAKENSIS=makensis
else
  N=Terceiros/nsis
  if [ ! -x "$N/usr/bin/makensis" ]; then
    mkdir -p "$N" && (cd "$N" && dnf download mingw32-nsis mingw-nsis-base >/dev/null &&
      for r in *.rpm; do rpm2cpio "$r" | cpio -idm --quiet; done)
  fi
  export NSISDIR=$PWD/$N/usr/share/nsis
  MAKENSIS=$PWD/$N/usr/bin/makensis
fi

mkdir -p dist
python3 - <<'PY'
from PIL import Image
Image.open("Ferramentas/icone.png").save("dist/icone.ico", sizes=[(16,16),(32,32),(48,48),(64,64),(128,128),(256,256)])
PY

ORIGEM=dist/windows-origem
rm -rf "$ORIGEM" && cp -a Builds/Windows "$ORIGEM"
rm -rf "$ORIGEM"/*DoNotShip*

SAIDA="dist/TrilhaDaInovacao-Setup-$VERSAO.exe"
"$MAKENSIS" -V2 -DVERSAO="$VERSAO" -DORIGEM="$PWD/$ORIGEM" -DICONE="$PWD/dist/icone.ico" \
  -DSAIDA="$PWD/$SAIDA" Ferramentas/pacotes/instalador-windows.nsi
rm -rf "$ORIGEM"
ls -lh "$SAIDA"

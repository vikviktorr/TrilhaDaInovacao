#!/usr/bin/env bash
# Instalador nativo do Fedora/RHEL: dist/trilha-da-inovacao-<versão>-1.x86_64.rpm
# Instala em /opt/trilha-da-inovacao, com lançador no menu e o comando
# "trilha-da-inovacao" (aceita -desktop para jogar sem óculos).
set -euo pipefail
cd "$(dirname "$0")/.."

VERSAO=$(grep -oP 'Versao = "\K[^"]+' Assets/TrilhaDaInovacao/Scripts/Editor/ConfiguradorXR.cs)
[ -x Builds/Linux/TrilhaDaInovacao.x86_64 ] || { echo "Rode antes: Ferramentas/build.sh linux"; exit 1; }

TOPO=$PWD/dist/rpmbuild
rm -rf "$TOPO" && mkdir -p "$TOPO"/{SOURCES,SPECS,BUILD,RPMS,SRPMS}
cp -a Builds/Linux "$TOPO/SOURCES/Linux"
rm -rf "$TOPO/SOURCES/Linux/TrilhaDaInovacao_BurstDebugInformation_DoNotShip"
cp Ferramentas/icone.png "$TOPO/SOURCES/"
cp Ferramentas/pacotes/trilha-da-inovacao.desktop "$TOPO/SOURCES/"
cp Ferramentas/pacotes/trilha-da-inovacao.spec "$TOPO/SPECS/"

rpmbuild --define "_topdir $TOPO" --define "versao $VERSAO" -bb "$TOPO/SPECS/trilha-da-inovacao.spec"
cp "$TOPO"/RPMS/x86_64/*.rpm dist/
tar -C Builds -czf "dist/trilha-da-inovacao-$VERSAO-linux-x86_64.tar.gz" Linux --exclude='*DoNotShip*'
ls -lh dist/*.rpm dist/*.tar.gz

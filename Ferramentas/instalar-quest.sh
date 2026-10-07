#!/usr/bin/env bash
# Instala o APK no Meta Quest 2 por cabo USB (sideload).
# No óculos: Modo Desenvolvedor ligado (app Meta Horizon no celular) e
# "Permitir depuração USB" aceito na primeira conexão.
set -euo pipefail
cd "$(dirname "$0")/.."

APK=${1:-$(ls dist/TrilhaDaInovacao-*.apk 2>/dev/null | tail -1)}
APK=${APK:-Builds/Android/TrilhaDaInovacao.apk}
[ -f "$APK" ] || { echo "APK não encontrado. Rode: Ferramentas/build.sh quest"; exit 1; }

ADB=$(command -v adb || true)
[ -z "$ADB" ] && ADB=$HOME/Unity/Hub/Editor/6000.0.83f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb
[ -x "$ADB" ] || { echo "adb não encontrado (instale android-tools ou o módulo Android do Unity)"; exit 1; }

"$ADB" devices
"$ADB" install -r -g "$APK"
echo "Instalado. No Quest: Biblioteca > Fontes desconhecidas > Trilha da Inovação."

#!/usr/bin/env bash
# Instala o APK no Meta Quest 2 por cabo USB (sideload).
# No óculos: Modo Desenvolvedor ligado (app Meta Horizon no celular) e
# "Permitir depuração USB" aceito na primeira conexão.
set -euo pipefail
cd "$(dirname "$0")/.."

# Sem argumento, instala o APK mais recente (por data) entre o build e as cópias em dist/:
# a cópia em dist/ é feita à mão e pode ter ficado para trás.
APK=${1:-$(ls -t Builds/Android/TrilhaDaInovacao.apk dist/TrilhaDaInovacao-*.apk 2>/dev/null | head -1 || true)}
[ -n "$APK" ] && [ -f "$APK" ] || { echo "APK não encontrado. Rode: Ferramentas/build.sh quest"; exit 1; }
echo "APK: $APK ($(date -r "$APK" '+%d/%m/%Y %H:%M'))"

# Avisa se as cenas mudaram depois do build: o APK não teria as mudanças.
CENA=$(ls -t Assets/Scenes/*.unity 2>/dev/null | head -1 || true)
if [ -n "$CENA" ] && [ "$CENA" -nt "$APK" ]; then
  echo "Aviso: $CENA é mais nova que o APK. Rode Ferramentas/build.sh quest para gerar um APK atualizado."
fi

ADB=$(command -v adb || true)
[ -z "$ADB" ] && ADB=$HOME/Unity/Hub/Editor/6000.0.83f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb
[ -x "$ADB" ] || { echo "adb não encontrado (instale android-tools ou o módulo Android do Unity)"; exit 1; }

"$ADB" devices
"$ADB" install -r -g "$APK"
echo "Instalado. No Quest: Biblioteca > Fontes desconhecidas > Trilha da Inovação."

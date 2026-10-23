#!/usr/bin/env bash
# QA no próprio Quest: roda a visita no óculos com o visitante simulado (como o
# -qa do PC) e baixa o relatório com FPS real, tempo de GPU, quadros perdidos,
# memória e estado térmico.
#
#   Ferramentas/qa-quest.sh [visitas]
#
# visitas: quantas visitas seguidas (padrão 1). Com 10 (~45 min) é o teste de
# longa duração que a Meta recomenda: compara memória, FPS e temperatura entre
# as visitas e testa o óculos largado no meio da visita.
#
# O óculos pode ficar parado na mesa: o script finge o sensor de proximidade
# para o Quest não dormir (e a visita não pausar), e desfaz isso no fim.
set -euo pipefail
cd "$(dirname "$0")/.."

PACOTE=com.fofuchosgames.trilhadainovacao
ATIVIDADE=com.unity3d.player.UnityPlayerActivity
VISITAS=${1:-1}
REMOTO=/sdcard/Android/data/$PACOTE/files/tour
SAIDA=Builds/QA-Quest/$(date +%Y%m%d-%H%M)

ADB=$(command -v adb || true)
[ -z "$ADB" ] && ADB=$HOME/Unity/Hub/Editor/6000.0.83f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb
[ -x "$ADB" ] || { echo "adb não encontrado (instale android-tools ou o módulo Android do Unity)"; exit 1; }
[ "$("$ADB" get-state 2>/dev/null || true)" = "device" ] || {
  echo "Quest não conectado. Ligue o cabo USB e aceite \"Permitir depuração USB\" no óculos."
  exit 1
}
"$ADB" shell pm path "$PACOTE" >/dev/null || { echo "App não instalado. Rode: Ferramentas/instalar-quest.sh"; exit 1; }

"$ADB" shell am broadcast -a com.oculus.vrpowermanager.prox_close >/dev/null
trap '"$ADB" shell am broadcast -a com.oculus.vrpowermanager.automation_disable >/dev/null 2>&1 || true' EXIT

"$ADB" shell rm -rf "$REMOTO"
"$ADB" logcat -c
"$ADB" shell am start -S -n "$PACOTE/$ATIVIDADE" -e args "-qa -visitas $VISITAS" >/dev/null
echo "QA no Quest: $VISITAS visita(s), cerca de $((VISITAS * 5)) min. O app fecha sozinho no fim."

# Espera o app abrir e depois fechar (Application.Quit), com folga de 8 min por visita.
for _ in $(seq 30); do "$ADB" shell pidof "$PACOTE" >/dev/null 2>&1 && break; sleep 1; done
LIMITE=$((SECONDS + VISITAS * 480 + 120))
while "$ADB" shell pidof "$PACOTE" >/dev/null 2>&1; do
  [ $SECONDS -lt $LIMITE ] || { echo "Tempo esgotado: o app não fechou. Veja o logcat."; break; }
  sleep 15
done

mkdir -p "$SAIDA"
"$ADB" logcat -d -s Unity > "$SAIDA/logcat.txt" || true
if "$ADB" pull "$REMOTO/relatorio-qa.md" "$SAIDA/" >/dev/null 2>&1; then
  echo "Relatório: $SAIDA/relatorio-qa.md"
  echo
  cat "$SAIDA/relatorio-qa.md"
else
  echo "O relatório não foi gerado. Log do Unity em $SAIDA/logcat.txt"
  exit 1
fi

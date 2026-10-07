#!/usr/bin/env bash
# Gera tudo pela linha de comando (Unity fechado):
#   Ferramentas/build.sh                 -> setup + Linux + Windows + Quest
#   Ferramentas/build.sh linux quest     -> só os alvos pedidos
#   SEM_SETUP=1 Ferramentas/build.sh ... -> pula a regeneração das cenas
set -euo pipefail
cd "$(dirname "$0")/.."

UNITY=${UNITY:-$HOME/Unity/Hub/Editor/6000.0.83f1/Editor/Unity}
ALVOS=("$@")
[ ${#ALVOS[@]} -eq 0 ] && ALVOS=(linux windows quest)
mkdir -p Logs

rodar() {
  local nome=$1; shift
  echo "==> $nome"
  if ! "$UNITY" -batchmode -nographics -quit -projectPath "$PWD" -logFile "Logs/build-$nome.log" "$@"; then
    echo "Falhou. Veja Logs/build-$nome.log"; grep -E "error|Trilha da Inova" "Logs/build-$nome.log" | tail -20; exit 1
  fi
  grep "\[Trilha da Inovação\]" "Logs/build-$nome.log" | tail -3 || true
}

[ -d Assets/TrilhaDaInovacao/Modelos/Kenney ] || Ferramentas/baixar-modelos.sh

if [ -z "${SEM_SETUP:-}" ]; then
  rodar setup -buildTarget "Linux64" -executeMethod TrilhaDaInovacao.EditorTools.PipelineDeSetup.Tudo
fi

M=TrilhaDaInovacao.EditorTools.PipelineDeSetup
for alvo in "${ALVOS[@]}"; do
  case "$alvo" in
    linux)   rodar linux   -buildTarget Linux64 -executeMethod $M.BuildLinux ;;
    windows) rodar windows -buildTarget Win64   -executeMethod $M.BuildWindows ;;
    quest)   rodar quest   -buildTarget Android -executeMethod $M.BuildQuest ;;
    *) echo "Alvo desconhecido: $alvo (use linux, windows, quest)"; exit 1 ;;
  esac
done

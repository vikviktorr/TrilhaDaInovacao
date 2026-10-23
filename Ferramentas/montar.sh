#!/usr/bin/env bash
# Monta o jogo do zero em qualquer máquina (Linux, macOS ou Windows com Git Bash),
# sem perguntas: feito para ser rodado por uma IA (ou por CI) depois do git clone.
#
#   Ferramentas/montar.sh                  # verifica tudo e gera o build da plataforma atual
#   Ferramentas/montar.sh linux quest      # alvos pedidos (linux, windows, quest)
#   Ferramentas/montar.sh --verificar      # só confere as dependências, não gera nada
#   Ferramentas/montar.sh --instalar-unity # instala o Unity certo pelo Unity Hub e segue
#
# Variáveis: UNITY=/caminho/do/Unity (editor), UNITYHUB=/caminho/do/unityhub.
# Códigos de saída: 0 = ok, 2 = falta dependência, 3 = Unity aberto no projeto,
# 1 = o build falhou (veja Logs/build-*.log).
# A última linha é sempre "MONTAR: OK ..." ou "MONTAR: FALHOU ...".
set -uo pipefail
cd "$(dirname "$0")/.."
RAIZ=$PWD

VERSAO=$(sed -n 's/^m_EditorVersion: //p' ProjectSettings/ProjectVersion.txt)
REVISAO=$(sed -n 's/^m_EditorVersionWithRevision: .*(\(.*\))/\1/p' ProjectSettings/ProjectVersion.txt)

VERIFICAR=; INSTALAR=; ALVOS=()
for arg in "$@"; do
  case "$arg" in
    --verificar) VERIFICAR=1 ;;
    --instalar-unity) INSTALAR=1 ;;
    linux|windows|quest) ALVOS+=("$arg") ;;
    -h|--help) sed -n '2,15p' "$0"; exit 0 ;;
    *) echo "Argumento desconhecido: $arg"; exit 2 ;;
  esac
done

falhou() { echo "MONTAR: FALHOU · $1"; exit "${2:-2}"; }
ok() { echo "  ok   $1"; }
aviso() { echo "  aviso $1"; }

case "$(uname -s)" in
  Linux*) SO=linux ;;
  Darwin*) SO=mac ;;
  MINGW*|MSYS*|CYGWIN*) SO=windows ;;
  *) falhou "sistema não suportado: $(uname -s)" ;;
esac
if [ ${#ALVOS[@]} -eq 0 ]; then
  case $SO in windows) ALVOS=(windows) ;; *) ALVOS=(linux) ;; esac
fi
echo "Trilha da Inovação · $SO · Unity $VERSAO ($REVISAO) · alvos: ${ALVOS[*]}"

# --- Ferramentas básicas -------------------------------------------------------
command -v git >/dev/null && ok "git" || falhou "instale o git"
command -v curl >/dev/null && command -v unzip >/dev/null && ok "curl e unzip" \
  || aviso "curl/unzip ausentes: só fazem falta se os modelos Kenney sumirem"

# --- Unity Hub e editor ------------------------------------------------------------
achar_hub() {
  [ -n "${UNITYHUB:-}" ] && { echo "$UNITYHUB"; return; }
  for h in "$(command -v unityhub 2>/dev/null)" /opt/unityhub/unityhub "$HOME/Applications/Unity Hub.AppImage" \
           "/Applications/Unity Hub.app/Contents/MacOS/Unity Hub" "/c/Program Files/Unity Hub/Unity Hub.exe"; do
    [ -n "$h" ] && [ -x "$h" ] && { echo "$h"; return; }
  done
}

achar_unity() {
  [ -n "${UNITY:-}" ] && { echo "$UNITY"; return; }
  local c
  for c in "$HOME/Unity/Hub/Editor/$VERSAO/Editor/Unity" "/opt/unity/editors/$VERSAO/Editor/Unity" \
           "/Applications/Unity/Hub/Editor/$VERSAO/Unity.app/Contents/MacOS/Unity" \
           "/c/Program Files/Unity/Hub/Editor/$VERSAO/Editor/Unity.exe"; do
    [ -x "$c" ] && { echo "$c"; return; }
  done
  # Pasta de instalação personalizada: pergunta ao Hub.
  local hub; hub=$(achar_hub)
  if [ -n "$hub" ]; then
    "$hub" "${SEP[@]}" --headless editors -i 2>/dev/null | grep -F "$VERSAO" | sed 's/.*installed at //' | head -1
  fi
}

MODULOS=()
for alvo in "${ALVOS[@]}"; do
  case $alvo in
    linux) MODULOS+=(linux-mono) ;;
    windows) MODULOS+=(windows-mono) ;;
    quest) MODULOS+=(android android-sdk-ndk-tools android-open-jdk) ;;
  esac
done
# No Windows e no macOS, o executável do Hub precisa de "--" antes de --headless.
[ $SO = linux ] && SEP=() || SEP=(--)
comando_hub() {
  local m; local args=""
  for m in "${MODULOS[@]}"; do args+=" --module $m"; done
  echo "unityhub ${SEP[*]:+${SEP[*]} }--headless install --version $VERSAO --changeset $REVISAO$args --childModules"
}

UNITY=$(achar_unity)
if [ -z "$UNITY" ] || [ ! -x "$UNITY" ]; then
  HUB=$(achar_hub)
  if [ -n "$INSTALAR" ] && [ -n "$HUB" ]; then
    echo "Instalando o Unity $VERSAO pelo Hub (alguns GB)..."
    "$HUB" "${SEP[@]}" --headless install --version "$VERSAO" --changeset "$REVISAO" \
      $(for m in "${MODULOS[@]}"; do printf -- '--module %s ' "$m"; done) --childModules \
      || falhou "o Unity Hub não conseguiu instalar o editor"
    UNITY=$(achar_unity)
  fi
  if [ -z "$UNITY" ] || [ ! -x "$UNITY" ]; then
    echo "Unity $VERSAO não encontrado."
    [ -z "$HUB" ] && echo "  1. Instale o Unity Hub: https://unity.com/download"
    echo "  2. Instale o editor (ou rode este script com --instalar-unity):"
    echo "     $(comando_hub)"
    echo "  3. Ative uma licença (Personal serve): abra o Hub uma vez e faça login."
    echo "  Ou aponte para um editor já instalado: UNITY=/caminho/Unity $0"
    falhou "Unity $VERSAO ausente"
  fi
fi
ok "Unity: $UNITY"
export UNITY

# Módulos de build de cada alvo, dentro da pasta do editor.
case $SO in
  mac) MOTORES="$(dirname "$UNITY")/../../../PlaybackEngines" ;;
  *) MOTORES="$(dirname "$UNITY")/Data/PlaybackEngines" ;;
esac
for alvo in "${ALVOS[@]}"; do
  case $alvo in
    linux) pasta=LinuxStandaloneSupport ;;
    windows) pasta=WindowsStandaloneSupport ;;
    quest) pasta=AndroidPlayer ;;
  esac
  # O alvo da própria plataforma vem com o editor.
  if [ -d "$MOTORES/$pasta" ] || { [ "$alvo" = "$SO" ]; }; then ok "módulo $alvo"
  else echo "Falta o módulo de build '$alvo'. Instale com:"; echo "  $(comando_hub)"; falhou "módulo $alvo ausente"; fi
done

# --- Conteúdo do repositório ---------------------------------------------------
[ -f Assets/TrilhaDaInovacao/Scripts/Editor/PipelineDeSetup.cs ] || falhou "rode na raiz do repositório TrilhaDaInovacao"
if [ -d Assets/TrilhaDaInovacao/Modelos/Kenney ]; then ok "modelos Kenney"
elif [ -z "$VERIFICAR" ]; then Ferramentas/baixar-modelos.sh || falhou "download dos modelos Kenney"
else aviso "modelos Kenney ausentes (serão baixados)"; fi
[ -d Assets/Sebraelab3D ] && ok "modelo do Sebraelab" || aviso "Assets/Sebraelab3D ausente: o hub usa a réplica de blocos"
ls Assets/TrilhaDaInovacao/Voz/*/*.ogg >/dev/null 2>&1 && ok "vozes do roteiro (já no repositório)" \
  || aviso "sem vozes: as legendas aparecem sem áudio"
command -v blender >/dev/null && ok "Blender (opcional: só para editar os modelos)" || true

# --- Unity aberto no projeto trava o batchmode ---------------------------------
if [ -f Temp/UnityLockfile ] && ps -eo args 2>/dev/null | grep -i -- "-projectpath" | grep -v grep | grep -qF "$RAIZ"; then
  falhou "feche o Unity Editor que está com este projeto aberto" 3
fi

if [ -n "$VERIFICAR" ]; then echo "MONTAR: OK · dependências conferidas (nada foi gerado)"; exit 0; fi

# --- Build -----------------------------------------------------------------------
# build.sh: vozes (se houver Piper), cenas geradas por código, builds pedidos.
# Primeira abertura do projeto importa os assets: pode levar de 5 a 20 minutos.
if ! Ferramentas/build.sh "${ALVOS[@]}"; then
  grep -h -iE "No valid Unity|license is not|Failed to activate" Logs/build-*.log 2>/dev/null | head -3 \
    && echo "Parece problema de licença: abra o Unity Hub, faça login e ative a licença Personal."
  falhou "build (veja Logs/build-*.log)" 1
fi

SAIDAS=()
for alvo in "${ALVOS[@]}"; do
  case $alvo in
    linux) SAIDAS+=(Builds/Linux/TrilhaDaInovacao.x86_64) ;;
    windows) SAIDAS+=(Builds/Windows/TrilhaDaInovacao.exe) ;;
    quest) SAIDAS+=(Builds/Android/TrilhaDaInovacao.apk) ;;
  esac
done
for s in "${SAIDAS[@]}"; do [ -f "$s" ] || falhou "build não gerou $s" 1; done
echo "MONTAR: OK · ${SAIDAS[*]}"

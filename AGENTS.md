# Instruções para agentes de IA

Trilha da Inovação: experiência em VR para o Meta Quest 2 com marcos tecnológicos do Maranhão de 1986 a 2025.
O projeto é Unity **6000.0.83f1** (revisão `dacc44548933`) com URP Mobile, OpenXR e rastreamento de mãos.
Também roda no PC, em VR ou em modo desktop.
Responda ao usuário em português.

## Montar numa máquina nova

Rode a partir da raiz do repositório. O script não faz perguntas.

```bash
Ferramentas/montar.sh --verificar        # confere git, Unity, módulos e assets; não gera nada
Ferramentas/montar.sh                    # build da plataforma atual (Linux ou Windows)
Ferramentas/montar.sh linux windows quest
Ferramentas/montar.sh --instalar-unity quest   # instala o Unity certo pelo Unity Hub e segue
```

- A última linha é sempre `MONTAR: OK · <saídas>` ou `MONTAR: FALHOU · <motivo>`.
- Códigos de saída: `0` ok, `2` falta dependência, `3` o Unity Editor está aberto com o projeto, `1` o build falhou.
- Se faltar o Unity, o script imprime o comando `unityhub --headless install ...` com os módulos de cada alvo:
  `linux-mono`, `windows-mono`, `android`, `android-sdk-ndk-tools` e `android-open-jdk`.
  Instalar o editor e ativar a licença exigem o usuário; peça a ele, sem tentar contornar.
- O batchmode precisa de licença ativa (Personal serve).
  Para ativá-la, abra o Unity Hub uma vez e faça login.
- Editor fora do lugar padrão: `UNITY=/caminho/do/Unity Ferramentas/montar.sh`.
- A primeira importação do projeto leva de 5 a 20 minutos.
  Rode o build em segundo plano e não o mate por estar "parado" antes de uns 25 minutos.
- No Windows, use o Git Bash.
  No macOS, use o bash 4 ou mais novo (`brew install bash`), porque `baixar-modelos.sh` usa arrays associativos.

Saídas:

| Alvo | Arquivo |
| --- | --- |
| Linux | `Builds/Linux/TrilhaDaInovacao.x86_64` |
| Windows | `Builds/Windows/TrilhaDaInovacao.exe` |
| Quest 2 | `Builds/Android/TrilhaDaInovacao.apk` |

Para instalar no óculos (Modo Desenvolvedor e cabo USB), use `Ferramentas/instalar-quest.sh`.

## Rodar do jeito padrão no Unity (editor)

1. No Unity Hub, use **Add → Add project from disk**, escolha a pasta do repositório e abra com o 6000.0.83f1.
2. Espere a importação terminar. Os modelos Kenney e o modelo do Sebraelab já estão no repositório.
3. Rode o menu **Trilha da Inovação → 0 · Configurar tudo (XR + cenas)**.
   Ele configura o OpenXR e gera as 8 cenas em `Assets/Scenes/`.
   Se o TextMesh Pro pedir recursos, use **Trilha da Inovação → Importar TMP Essentials**.
4. Abra `Assets/Scenes/00_Bootstrap.unity` e aperte **Play**.
   A cena carrega o hub e as épocas de forma aditiva.
   Sem óculos, o jogo entra em modo desktop:

   | Ação | Comando |
   | --- | --- |
   | Andar | WASD |
   | Olhar | Botão direito ou Tab |
   | Tocar | Clique |
   | Pular cena | N |
   | Reiniciar a visita | R |
   | Calibrar o piso | C |

5. Para gerar os builds pelo editor, use os itens **Trilha da Inovação → 3 · Build Linux / Windows / Quest 2 (APK)**.

Pela linha de comando, com o editor fechado, o equivalente é `Ferramentas/build.sh [linux|windows|quest]`.
`SEM_SETUP=1` pula a regeneração das cenas.

### Problemas comuns no editor

| Sintoma | Causa | O que fazer |
| --- | --- | --- |
| Scene/Game view com chiado vermelho e azul no céu e no chão | Driver OpenGL do Mesa na Intel (a barra de título mostra `<OpenGL 4.5>`) desenha lixo de memória | Feche o Unity. No Unity Hub, em **Projects → ⋮ → Add command line arguments**, coloque `-force-vulkan` e reabra. A barra de título deve mostrar `<Vulkan>`. Se não resolver, teste `-force-glcore45` ou atualize o Mesa. |
| Hierarchy só com `Untitled`, Main Camera e Directional Light | É a cena vazia padrão do Unity; o jogo não está aberto | Abra `Assets/Scenes/00_Bootstrap.unity` e aperte **Play**. |
| Época ou hub sem luz, rig ou interface | As cenas são aditivas e foram abertas sozinhas | Rode sempre a partir de `00_Bootstrap.unity`. |
| Materiais rosa, modelos faltando ou erros que somem depois | A primeira importação (`Library/` vazia) ainda está rodando | Espere a importação terminar (5 a 20 minutos) e rode **Trilha da Inovação → 0 · Configurar tudo (XR + cenas)**. |
| Projeto "novo" e lento logo depois de um `git clone` | O clone foi feito dentro da pasta de outro clone e o Hub abriu o aninhado | Abra a pasta do repositório que já tem a `Library/` importada e apague o clone aninhado. |
| Aviso `CS0618 ... k_SettingsKey is obsolete` em `ConfiguradorXR.cs` | API depreciada do XR Plug-in Management 4.7 | Só aviso; não atrapalha. |
| Pasta vazia `Assets/XR 1` | Criada pelo editor durante a configuração do XR | Pode ignorar; pasta vazia não entra no git. |
| `GLib-GIO-CRITICAL`, `libxml2.so.16` ou shaders `Terrain/Lit`/`SpeedTree7` "not found" no `Editor.log` | Mensagens normais do Unity com URP no Linux | Pode ignorar. |

## Rodar no Meta Quest 2

O jogo roda direto no óculos (APK Android, standalone), sem PC.
Pacote: `com.fofuchosgames.trilhadainovacao`.
Atividade: `com.unity3d.player.UnityPlayerActivity`.

### Preparar o óculos (uma vez, feito pelo usuário)

Peça ao usuário estes passos, porque exigem a conta Meta e o óculos na mão:

1. **Modo Desenvolvedor:** no app Meta Horizon do celular, abra **Dispositivos → o óculos → Configurações do headset → Modo Desenvolvedor** e ligue.
   A conta precisa pertencer a uma organização de desenvolvedor, criada grátis em developers.meta.com.
2. **Cabo USB-C** ligado ao computador.
   No óculos, aceite **"Permitir depuração USB"** e marque "Sempre permitir deste computador".
3. **Rastreamento de mãos** ligado no óculos: **Configurações → Movimentação → Rastreamento de mãos**.
   Os controles também funcionam.

Para conferir a conexão, rode `adb devices`.
O óculos deve aparecer como `device`; se aparecer como `unauthorized`, falta aceitar a depuração no óculos.
Os scripts acham o `adb` do sistema ou o do módulo Android do Unity:
`~/Unity/Hub/Editor/6000.0.83f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb`.

### Gerar, instalar e abrir

```bash
Ferramentas/montar.sh quest         # gera Builds/Android/TrilhaDaInovacao.apk (precisa do módulo Android)
Ferramentas/instalar-quest.sh       # adb install -r -g do APK mais recente; avisa se as cenas forem mais novas que o APK
adb shell am start -n com.fofuchosgames.trilhadainovacao/com.unity3d.player.UnityPlayerActivity   # abre o app
```

- O build Android usa IL2CPP com um job só, para caber em máquinas com 8 GB de RAM.
  Leva de 10 a 30 minutos; rode em segundo plano.
- Depois de mudar código ou cenas, gere o APK de novo antes de instalar.
  Um APK antigo não tem as mudanças.
- No óculos, o app fica em **Biblioteca → Fontes desconhecidas → Trilha da Inovação**.
- Para desinstalar: `adb uninstall com.fofuchosgames.trilhadainovacao`.

### Espaço físico e operação

- A visita é feita a pé: o visitante anda entre 7 marcas no chão, num U de cerca de 4,2 × 1,75 m.
  As medidas estão em `LayoutTrilha.Marcas`.
- Configure o limite (Guardian) em **escala de ambiente**, cobrindo esse espaço.
- **Calibrar o piso:** com o app aberto, fique de pé na marca INÍCIO, olhando para dentro do U, e segure o botão **A** do controle por 2 s.
  A calibração fica salva.
- Monitor:
  - **B** segurado por 1 s pula a cena;
  - **Y** segurado por 3 s reinicia a visita.
- Ao tirar o óculos, a visita pausa.
  Se o óculos voltar em até 45 s, ela continua; depois disso, recomeça para o próximo visitante.

### Logs e testes no óculos

```bash
adb logcat -s Unity                 # log do jogo ao vivo
Ferramentas/qa-quest.sh [N]         # QA automática no óculos (N visitas); o relatório vai para Builds/QA-Quest/<data>/
adb pull /sdcard/Android/data/com.fofuchosgames.trilhadainovacao/files/sessoes.csv   # tempos de cada visita
```

- O `qa-quest.sh` funciona com o óculos parado na mesa, porque finge o sensor de proximidade.
- Ele mede o FPS real (a meta é 72 Hz), o tempo de GPU, os quadros perdidos e o estado térmico.
- O `-tour` e o `-qa` também podem ser passados ao app à mão: `adb shell am start -S -n <pacote>/<atividade> -e args "-qa"`.

### Alternativa: Quest ligado ao PC (Link)

Pelo Quest Link ou Air Link, no Windows, o build `Builds/Windows/TrilhaDaInovacao.exe` e o **Play** do editor abrem em VR no óculos.
Para isso, o runtime OpenXR ativo tem de ser o da Meta.
Use esse caminho para iterar rápido.
A medição de desempenho vale só no APK, rodando no próprio óculos.

## Regras do projeto

- **As cenas são geradas por código.**
  Não edite `Assets/Scenes/*.unity` à mão: o próximo setup sobrescreve a mudança.
  Edite o construtor, que fica em `Assets/TrilhaDaInovacao/Scripts/Editor/`:
  - `ConstrutorDeCenas.cs`: helpers
  - `ConstrutorDeCenas.Epocas.cs`: uma função `ConstruirAAAA` por época
  - `ConstrutorDeCenas.Modelos.cs`: modelos e decoração
  
  Depois de editar, rode o setup.
- **Referencial das épocas:** cada época é montada num `Quadro(marca)`.
  O visitante fica na origem, `+z` é para onde ele olha e `+x` aponta para a próxima marca.
- **O que acontece em cada cena** fica em `Scripts/Runtime/Sequencias/CenaN*.cs`, como coroutines.
  O construtor liga os campos dessas classes aos objetos da cena.
- **Falas:** o texto original fica em `Scripts/Editor/Roteiro.cs` e o texto revisado em `Assets/TrilhaDaInovacao/Roteiro/*.asset`.
  O setup não sobrescreve o texto revisado.
  Para mudar uma fala que já existe, acrescente uma entrada em `Roteiro.Revisoes` com o id, o texto antigo e o texto novo.
- **Vozes (dublagem por IA):** os arquivos `.ogg` ficam em `Assets/TrilhaDaInovacao/Voz/` e já estão no repositório.
  Para refazer uma fala, rode o setup para gravar o `.asset`, depois `Ferramentas/voz/gerar_vozes.py` (Piper e ffmpeg), e o setup de novo para ligar o áudio.
  Sem o Piper, deixe como está: a legenda aparece sem a voz nova.
- **Modelos próprios** (telefone, rádio, impressora, drone, foguete, console e radar CUPRI e outros) são scripts Blender em `Ferramentas/blender/*.py`, que exportam FBX para `Assets/TrilhaDaInovacao/Modelos/`.
  Para refazer um, rode `blender -b -P Ferramentas/blender/<script>.py`.
- **Orçamento do Quest 2:** até ~100 draw calls por cena.
  O setup mescla os renderers estáticos.
  Objetos com MonoBehaviour, ou referenciados por um, ficam separados.
- **Não versionar:** `Library/`, `Temp/`, `Builds/`, `Logs/`, `UserSettings/`, `dist/`, `Terceiros/`, `*.csproj` e `*.apk` (já estão no `.gitignore`).
- **Git:** commit e push só quando o usuário pedir.

## Testar

```bash
Builds/Linux/TrilhaDaInovacao.x86_64 -tour <pasta>              # visita automática com screenshots
Builds/Linux/TrilhaDaInovacao.x86_64 -qa <pasta> [-visitas N]   # gera <pasta>/relatorio-qa.md
Ferramentas/qa-quest.sh [N]                                     # a mesma QA no Quest, por USB
```

- Uma visita leva cerca de 5 minutos.
- O relatório traz o tempo por cena, FPS, draw calls, ergonomia, a pausa ao tirar o óculos, o botão PRÓXIMO, a travessia dos portais e os erros do log.
- Durante a `-qa`, as teclas do operador ficam desligadas.
- Confira também as screenshots, porque o relatório não vê objetos atravessados nem textos ilegíveis.
- No setup, procure em `Logs/build-setup.log` as linhas `[Trilha da Inovação] ... atravessa ...` (colisões entre objetos) e `error CS` (erros de compilação).

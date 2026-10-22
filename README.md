# Trilha da Inovação — marcos tecnológicos do Maranhão (1986–2025)

Experiência em realidade virtual para o Meta Quest 2, com rastreamento de mãos, no estande do Sebraelab (São Luís).
O visitante caminha de verdade por seis marcas no piso. Em cada marca o mundo muda de época e mostra um marco da tecnologia maranhense.

**Fofucho's Games Group**: Pedro Victor Santos Velôso, Davi Chaves de Oliveira, Gabriel Araujo Meneses,
Kauã Carneiro de Morais, Lucas Gomes Antunes e Kalil Ramos Caldeira.
Disciplina Tecnologias Emergentes, prof. Kassio Romulo Lima Souas.

O projeto implementa o documento *Trilha da Inovação VR — Cenários e Roteiro de Produção*, derivado do Paper Check 1 (v3).
O projeto da P1 (feira com estandes) continua no histórico do git, na tag `p1-feira-estandes`.

## A visita (~4 min 45 s)

| Cena | Marca | Ano | Marco | Gatilho do visitante |
| --- | --- | --- | --- | --- |
| 0 | Início | — | Abertura e calibração das mãos | Tocar "Começar" |
| 1 | 1 | 1986 | Central Trópico-R da TELMA | Tocar o telefone |
| 2 | 2 | 1994 | Campanha Guará, Alcântara | Pisar na marca do radar |
| 3 | 3 | 2009 | Rede Metropolitana de São Luís (RNP) | Tocar o ponto no mapa |
| 4 | 4 | 2018 | Laboratório Aberto do SENAI | Tocar a lâmpada |
| 5 | 5 | 2022 | 5G em 3,5 GHz | Aproximar-se da torre |
| 6 | 6 | 2023–2025 | HANBIT-TLV (SISNAV) e HANBIT-Nano (PION-BR2) | Assistir à decolagem |
| 7 | 6 | — | Encerramento | Retirar o óculos |

Todo gatilho tem plano B: o guia repete a instrução em 6 s e a cena avança sozinha em 10 s.

## Mapa físico

As marcas formam um U numa área livre de **5 × 4 m** (provisório, é preciso medir o Sebraelab).
As coordenadas ficam em `Scripts/Runtime/LayoutTrilha.cs`, e é só ali que se muda o layout: a marca virtual, o gatilho e o cenário acompanham.
O perímetro da área aparece fraco no chão em todas as épocas.

A abertura e o encerramento (cenas 0 e 7) acontecem dentro do **modelo 3D do Sebraelab** (`Assets/Sebraelab3D/SM_Combined.fbx`), em escala 1:1.
Hoje a trilha fica no salão norte: o visitante começa de frente para a mesa de madeira, com a TV, a caixa de som e as lousas ao fundo, e o guia o espera atrás da mesa.
O U cabe entre a parede de ripas, o banco junto ao vidro e a mesa.
O ponto do modelo que fica no centro do U é `LayoutTrilha.CentroNoSebraelab`. Para usar outra área do Sebraelab, mude só esse valor.
Quando o visitante chega à primeira marca, o Sebraelab dá lugar às épocas.
No modo desktop, paredes e móveis de todos os cenários têm colisão (camada `Cenario`): o visitante desliza neles em vez de atravessá-los.

```
 z  ┌──────────────────────────────────────┐
 ▲  │ SAÍDA ◄─ 2025 ◄── 2022 ◄── 2018     │   linha de cima (olham para o sul)
 │  │                              ▲       │
 │  │        (centro do U: guia     │       │
 │  │         e objetos de toque)   │       │
 │  │ INÍCIO ─► 1986 ─► 1994 ─► 2009       │   linha de baixo (olham para o norte)
    └──────────────────────────────────────┘ ─► x
```

## Estrutura

```
Assets/
├── Sebraelab3D/            pacote SebraeLabUnityAssets (modelo do Sebraelab, FBX + texturas)
├── Scenes/                 GERADAS pelo construtor (não editar à mão)
│   ├── 00_Bootstrap        rig XR, mãos, guia, legenda, marcas, diretor da visita
│   ├── 01_Hub              cenas 0 e 7, dentro do Sebraelab
│   └── 02…07_<época>       uma cena aditiva por época
├── TextMesh Pro/           TMP Essential Resources
└── TrilhaDaInovacao/
    ├── Roteiro/            SceneScript por cena: falas = legendas (EDITÁVEIS)
    ├── Modelos/Kenney/     modelos 3D CC0 usados nos cenários
    ├── Gerado/             materiais, ícone e malhas gerados
    └── Scripts/
        ├── Runtime/        VisitDirector, FloorMarker, PokeTrigger, ProximityTrigger,
        │   ├── Sequencias/ ScreenFader, SubtitleController, GuideCharacter, HandPoints,
        │   └── Efeitos/    DesktopRig, XRStartup, SessionLogger, OperatorPanel…
        └── Editor/         construtor de cenas, roteiro, XR, ícone, builds
Ferramentas/                build, download de modelos, instaladores
```

**Roteiro:** as falas ficam em `Assets/TrilhaDaInovacao/Roteiro/*.asset`. Edite o texto no Inspector e arraste a voz gravada para o campo `audio`.
Sem áudio, a legenda fica na tela pelo tempo calculado (≈2,6 palavras/s).
Regenerar as cenas não sobrescreve o texto revisado.

**Diferença em relação ao documento:** as sequências são coroutines (`Scripts/Runtime/Sequencias/`) e não Timelines.
Assim dá para gerar tudo por código e mudar o roteiro sem tocar em assets binários.

## Modelos 3D

| Fonte | Licença | Como entra no projeto |
| --- | --- | --- |
| Sebraelab (pacote `SebraeLabUnityAssets.unitypackage`, da equipe) | Próprio | Importado em `Assets/Sebraelab3D`. O hub usa `SM_Combined.fbx`; sem o pacote, o construtor volta a uma réplica de blocos |
| [Kenney](https://kenney.nl/assets/category:3D): Furniture, Space, Nature, City Commercial, Factory, Mini Characters | CC0 (domínio público) | `Ferramentas/baixar-modelos.sh` baixa e copia só os FBX usados |
| Sketchfab, TurboSquid, Fab | Varia por modelo (CC-BY exige crédito) | Exigem login; ver "Pendências" |

Os objetos históricos específicos (bastidores Trópico-R, telefones, HANBIT, SISNAV, torre 5G) continuam modelados no código, como pede a lista de assets do documento.
Ainda é preciso substituí-los por modelos feitos a partir de fotos.

## Como gerar

Com o Unity 6000.0.83f1 (módulos Linux, Windows e Android) e o editor **fechado**:

```bash
Ferramentas/build.sh                 # gera as cenas + builds Linux, Windows e Quest
Ferramentas/build.sh linux           # só um alvo
Ferramentas/empacotar-rpm.sh         # dist/trilha-da-inovacao-<v>-1.x86_64.rpm (+ .tar.gz)
Ferramentas/empacotar-windows.sh     # dist/TrilhaDaInovacao-Setup-<v>.exe (NSIS)
Ferramentas/instalar-quest.sh        # adb install do APK no Quest 2 (Modo Desenvolvedor)
```

No editor: menu **Trilha da Inovação → 0 · Configurar tudo** e depois os itens **3 · Build**.

## Controles

| Quem | Ação | Comando |
| --- | --- | --- |
| Visitante (Quest) | Tocar | Encostar a ponta do indicador (ou o controle) |
| Visitante (Quest) | Andar | Caminhar de verdade até a marca que brilha |
| Monitor | Calibrar o piso | De pé na marca INÍCIO, olhando para dentro do U: segurar **A** por 2 s (ou **C** no teclado). Fica salvo |
| Monitor | Pular cena | Segurar **B** por 1 s (ou **N**) |
| Monitor | Reiniciar visita | Segurar **Y** por 3 s (ou **R**) |
| PC sem óculos (`-desktop`) | Andar / olhar / tocar | WASD · botão direito ou Tab · clique |

O jogo abre em VR quando acha um runtime OpenXR (Quest Link, SteamVR, ALVR, Monado). Sem runtime, abre em modo desktop.

## Testes da metodologia

- Cada visita grava `sessoes.csv` (sessão, cena, duração, previsto, gatilho `visitante`/`auto`/`pulado`) em `persistentDataPath`.
  No Quest: `/sdcard/Android/data/com.fofuchosgames.trilhadainovacao/files/`. No Linux: `~/.config/unity3d/Fofucho's Games Group/Trilha da Inovação/`.
- `TrilhaDaInovacao.x86_64 -tour <pasta>` percorre a visita sozinho e salva screenshots dos momentos-chave.
- `TrilhaDaInovacao.x86_64 -qa <pasta>` faz o mesmo e grava `relatorio-qa.md`: tempo de cada cena contra o roteiro, FPS, draw calls, triângulos e luzes (orçamento do Quest 2), ergonomia (alvos de toque, legendas de até 2 linhas, corredor livre) e erros do log.

## Pendências (do documento)

- [ ] Medir a área livre do Sebraelab e ajustar `LayoutTrilha` (hoje 5 × 4 m, no salão norte do modelo: `CentroNoSebraelab`)
- [ ] Nomes das seis instituições da Rede Metropolitana (RNP, 2009) para rotular os pinos
- [ ] Valores e fonte da comparação 3G/4G/5G (hoje o painel é ilustrativo)
- [ ] Gravar as falas do guia e definir o nome e o visual dele
- [ ] Modelos de referência: central Trópico-R e foguetes HANBIT (Sketchfab/TurboSquid/Fab ou modelagem própria)
- [ ] Bake de iluminação e medição a 72 Hz no OVR Metrics Tool
- [ ] Testar com visitantes que nunca usaram VR

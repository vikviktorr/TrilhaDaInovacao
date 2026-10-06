# Trilha da Inovação — A Feira Tecnológica do Maranhão

Experiência de Realidade Virtual para o desafio **Vitrine de Soluções do Sebrae**
(tema integrador "Experiência Imersiva no Sebraelab").

**Fofucho's Games Group** — Pedro Victor Santos Veloso, Gabriel Araujo Menezes,
Kauã Carneiro de Morais, Davi Chaves de Oliveira, Lucas Gomes Antunes, Kalil Ramos.
UNDB — Engenharia de Software · Tecnologias Emergentes.

Este projeto implementa o escopo definido na P1 — Coleta de Dados.

## Ambiente

| Item | Valor |
| --- | --- |
| Unity | 6000.0.83f1 (Unity 6 LTS) |
| Render pipeline | URP 17 (perfil Mobile para o Quest 2) |
| XR | OpenXR + XR Plug-in Management + XR Interaction Toolkit 3.6 |
| Alvo | Meta Quest 2 (Android, ARM64, IL2CPP, Vulkan) |
| Locomoção | Room-scale — caminhada física real, **sem teleporte** |

> ⚠️ **Falta o Android Build Support neste editor.** Hoje só o
> LinuxStandaloneSupport está instalado, então o projeto roda em PC VR mas
> ainda não builda para o Quest 2 — e as features OpenXR de Android
> (Meta Quest Support) não chegam a ser criadas. Instale pelo Unity Hub:
> *Installs → 6000.0.83f1 → Add modules → Android Build Support*
> (com OpenJDK e Android SDK & NDK Tools) e rode o menu
> **Trilha da Inovação → 1 · Configurar XR** de novo.

## Cenas (escopo: 3 cenas)

1. `01_BoasVindas_Tutorial` — hall de entrada; o NPC narrador explica o objetivo
   em ~15 s e avisa que o deslocamento é caminhando de verdade pela sala.
2. `02_Feira_TrilhaDaInovacao` — hub único com os 6 estandes cronológicos e o
   Mural do Conhecimento. Sem carregamento entre estandes.
3. `03_Encerramento_Creditos` — créditos com o convite para conhecer o
   Sebraelab, e reinício automático para o próximo visitante da fila.

## Pontos de interação (7, acima do mínimo de 5)

6 NPCs dublados (um por estande) + o Mural do Conhecimento.

## Sistema implementado

**Diálogo com NPCs integrado a uma máquina de estados** — o contrato exigido
pela avaliação:

```
Bloqueado ──(usuário se aproxima)──▶ Visitado ──(fala termina)──▶ Concluído
```

- `ProgressoExperiencia` guarda o estado dos estandes e emite os eventos.
  Persiste entre cenas (`DontDestroyOnLoad`).
- `NpcEstande` detecta a aproximação por distância (com histerese, para o NPC
  não gaguejar na borda do raio), toca a dublagem espacializada e exibe a
  legenda; ao terminar, marca o estande como concluído.
- `MuralDoConhecimento` escuta esses eventos e acende o setor correspondente.
- Quando o último setor acende, `CelebracaoFinal` dispara partículas e luz e,
  ao fim, carrega os créditos.

## Estrutura

```
Assets/TrilhaDaInovacao/
├── Data/Estandes/      6 assets EstandeData (roteiro provisório por década)
├── Audio/Narracao/     dublagens da equipe (a gravar)
├── Materials/          materiais do whitebox
└── Scripts/
    ├── Runtime/        lógica da experiência
    └── Editor/         configuração de XR e construtor de cenas
```

## Regerar o whitebox

No menu do editor: **Trilha da Inovação → 0 · Configurar tudo (XR + cenas)**.

Isso reconfigura o OpenXR, recria os assets dos estandes e reconstrói as três
cenas do zero. Também roda por linha de comando:

```bash
~/Unity/Hub/Editor/6000.0.83f1/Editor/Unity -batchmode -nographics -quit \
  -projectPath . -executeMethod TrilhaDaInovacao.EditorTools.PipelineDeSetup.Tudo
```

> As cenas são **geradas**. Ao editar uma cena à mão, pare de rodar o construtor
> ou passe as mudanças para `ConstrutorDeCenas.cs` — ele sobrescreve tudo.

## Antes da demonstração no Sebrae

- [ ] **Medir a sala in loco.** O layout inteiro sai de `LarguraSala` e
      `ProfundidadeSala` em `ConstrutorDeCenas.cs` (hoje 6 × 8 m, provisório).
      A modelagem precisa ser 1:1 com o espaço físico.
- [ ] Configurar o Guardian do Quest 2 com o perímetro real da sala.
- [ ] Remover obstáculos físicos do caminho entre os estandes.
- [ ] Marcar o percurso com fita no chão e testar com a equipe.
- [ ] Gravar as dublagens e ligá-las em cada `EstandeData`; enquanto não
      existem, a legenda usa `duracaoSemAudio` como cronômetro.
- [ ] Revisar o roteiro definitivo dos 6 estandes.
- [ ] Trocar o whitebox por assets low poly e bakear a iluminação.

# Radar CUPRI (Cornell University Portable Radar Interferometer) para a Cena 2 ·
# 1994 (Campanha Guará), low poly.
#
#   blender -b -P Ferramentas/blender/radar_cupri.py -- [saida.fbx] [previa.png]
#
# O radar que acompanhou a ionosfera na Campanha Guará: um radar de
# retroespalhamento coerente em VHF (50 MHz, λ ≈ 6 m), portátil, que a Cornell
# montou perto do Centro de Lançamento de Alcântara de meados de agosto a meados
# de outubro de 1994 (Swartz, GRL 1997; Swartz e Woodman, GRL 1998). Ele não
# gira: as antenas olham para o zênite e o radar mede os ecos da ionosfera
# (eletrojato equatorial e as plumas do spread F) em função da altura e do tempo.
#
# Como nos radares de 50 MHz da época, as antenas são Yagi de 5 elementos com o
# eixo na vertical, em dois arranjos quadrados de 4 × 4 lado a lado (transmissão
# e recepção) e dois grupos de 2 × 2 afastados, que só recebem: a diferença de
# fase entre eles dá a posição do eco (o "interferômetro"). As medidas seguem o
# comprimento de onda: elementos de meia onda (~2,6 a 3 m), 1,2 m entre eles e
# ~3,5 m entre as Yagis. O transmissor, o receptor e o computador ficam num
# abrigo transportável, ligado às antenas por cabos coaxiais no chão.
#
# A contagem exata de antenas do CUPRI em Alcântara não foi encontrada nas
# fontes; o arranjo segue o padrão dos radares de 50 MHz do mesmo tipo.
#
# Medidas em metros, Z para cima. A origem fica no centro do piso do abrigo. Com
# o giro de 180° do construtor de cenas, (x, y) do Blender viram (x, z) no Unity:
#   centro dos arranjos 4 × 4: (ARRANJO_X, ARRANJO_Y ± ARRANJO_DY)
#   grupos do interferômetro:  (ARRANJO_X, INTERF_Y[k])
#   altura total: ALTURA (topo das Yagis)
import math
import sys
from pathlib import Path

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from comum import *  # noqa: E402,F403

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
SAIDA = Path(args[0]) if args else RAIZ / "Assets/TrilhaDaInovacao/Modelos/Guara/radar-cupri.fbx"
PREVIA = Path(args[1]) if len(args) > 1 else None

limpar_cena()
ALUMINIO = material("Aluminio", (0.72, 0.74, 0.76))
MASTRO = material("Mastro", (0.45, 0.46, 0.44))
PRETO = material("Preto", (0.05, 0.05, 0.06))
BRANCO = material("Branco", (0.86, 0.87, 0.84))
CINZA = material("Cinza", (0.5, 0.52, 0.52))
ESCURO = material("Escuro", (0.18, 0.19, 0.2))
CONCRETO = material("Concreto", (0.62, 0.6, 0.56))
LARANJA = material("Laranja", (0.9, 0.42, 0.08))

# Yagi vertical: mastro até BASE_BOOM, boom até ALTURA, elementos nas alturas abaixo.
BASE_BOOM = 1.2
ALTURA = 6.4
ELEMENTOS = [(1.4, 3.0), (2.6, 2.84), (3.8, 2.7), (5.0, 2.64), (6.2, 2.58)]   # (altura, comprimento)
PASSO = 3.5            # entre Yagis do arranjo (~0,6 λ)
ARRANJO_X = 7.0        # distância do abrigo aos arranjos, para o lado +X
ARRANJO_Y = 11.0
ARRANJO_DY = 6.5       # metade da distância entre os centros dos dois arranjos 4 × 4
INTERF_Y = (-6.0, 28.0)
ABRIGO = (2.4, 4.6, 2.5)   # largura (x), comprimento (y), altura


def caixa(nome, centro, tamanho, mat):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation(centro) @ Matrix.Diagonal((*tamanho, 1.0)))
    return objeto_de_bmesh(nome, bm, mat)


def barra(nome, a, b, espessura, mat):
    """Caixa fina de a até b (seção quadrada), para cabos e travessas."""
    a, b = Vector(a), Vector(b)
    d = b - a
    ob = caixa(nome, (0, 0, 0), (espessura, espessura, d.length), mat)
    ob.matrix_world = Matrix.Translation((a + b) / 2) @ d.to_track_quat("Z", "Y").to_matrix().to_4x4()
    return ob


def yagi(x, y, pecas):
    pecas.append(caixa("Mastro", (x, y, BASE_BOOM / 2), (0.08, 0.08, BASE_BOOM), MASTRO))
    pecas.append(caixa("Sapata", (x, y, 0.03), (0.3, 0.3, 0.06), CONCRETO))
    pecas.append(caixa("Boom", (x, y, (BASE_BOOM + ALTURA) / 2), (0.05, 0.05, ALTURA - BASE_BOOM), ALUMINIO))
    for k, (z, comp) in enumerate(ELEMENTOS):
        pecas.append(caixa(f"Elemento {k}", (x, y, z), (comp, 0.035, 0.035), ALUMINIO))
    # Balun do dipolo alimentado e o cabo descendo pelo mastro.
    pecas.append(caixa("Balun", (x, y - 0.06, ELEMENTOS[1][0] - 0.12), (0.12, 0.08, 0.16), PRETO))
    pecas.append(caixa("Cabo do mastro", (x + 0.05, y - 0.06, (ELEMENTOS[1][0] - 0.2) / 2),
                       (0.02, 0.02, ELEMENTOS[1][0] - 0.2), PRETO))


def arranjo(cx, cy, n, pecas):
    """n × n Yagis; cabos de cada fileira até a caixa de fase na borda voltada ao abrigo."""
    meio = (n - 1) / 2
    borda = cx - (meio + 0.6) * PASSO
    for i in range(n):
        y = cy + (i - meio) * PASSO
        for j in range(n):
            yagi(cx + (j - meio) * PASSO, y, pecas)
        pecas.append(barra("Cabo da fileira", (borda, y - 0.1, 0.02), (cx + meio * PASSO, y - 0.1, 0.02), 0.03, PRETO))
    pecas.append(barra("Cabo da caixa", (borda, cy - meio * PASSO - 0.1, 0.02), (borda, cy + meio * PASSO - 0.1, 0.02),
                       0.04, PRETO))
    pecas.append(caixa("Caixa de fase", (borda, cy, 0.25), (0.5, 0.7, 0.5), CINZA))
    pecas.append(caixa("Base da caixa", (borda, cy, 0.02), (0.7, 0.9, 0.04), CONCRETO))
    return Vector((borda, cy, 0.02))


pecas = []
# --- Abrigo transportável: transmissor de 50 MHz, receptores e o computador. --
L, C, H = ABRIGO
pecas.append(caixa("Abrigo", (0, 0, 0.35 + H / 2), (L, C, H), BRANCO))
pecas.append(caixa("Teto", (0, 0, 0.35 + H + 0.04), (L + 0.1, C + 0.1, 0.08), CINZA))
for sx in (-1, 1):
    for sy in (-1, 1):
        pecas.append(caixa("Macaco", (sx * (L / 2 - 0.15), sy * (C / 2 - 0.2), 0.18), (0.14, 0.14, 0.36), ESCURO))
# Frisos verticais das chapas, nas paredes compridas.
for sx in (-1, 1):
    for k in range(1, 6):
        y = -C / 2 + k * C / 6
        pecas.append(caixa("Friso", (sx * (L / 2 + 0.01), y, 0.35 + H / 2), (0.03, 0.05, H - 0.1), BRANCO))
# Porta, degrau e a caixa de entrada dos cabos do console na parede -X (voltada ao deck).
pecas.append(caixa("Porta", (-L / 2 - 0.015, -C / 2 + 0.9, 0.35 + 1.0), (0.03, 0.9, 1.95), CINZA))
pecas.append(caixa("Maçaneta", (-L / 2 - 0.05, -C / 2 + 1.25, 0.35 + 1.0), (0.05, 0.12, 0.04), PRETO))
pecas.append(caixa("Degrau", (-L / 2 - 0.35, -C / 2 + 0.9, 0.17), (0.6, 0.9, 0.34), ESCURO))
pecas.append(caixa("Entrada de cabos", (-L / 2 - 0.08, C / 2 - 0.9, 0.6), (0.16, 0.4, 0.3), CINZA))
# Ar-condicionado na parede de fundo e a faixa de aviso.
pecas.append(caixa("Ar-condicionado", (0, C / 2 + 0.25, 0.35 + H - 0.7), (1.0, 0.5, 0.7), CINZA))
for k in range(5):
    pecas.append(caixa("Grade", (-0.4 + k * 0.2, C / 2 + 0.505, 0.35 + H - 0.7), (0.05, 0.01, 0.5), ESCURO))
pecas.append(caixa("Faixa", (-L / 2 - 0.006, 0, 0.35 + H - 0.25), (0.01, C - 0.2, 0.12), LARANJA))
# Mastro do para-raios no teto.
pecas.append(caixa("Para-raios", (L / 2 - 0.2, -C / 2 + 0.2, 0.35 + H + 0.9), (0.04, 0.04, 1.8), ALUMINIO))

# --- Antenas ------------------------------------------------------------------
saidas = [arranjo(ARRANJO_X, ARRANJO_Y + s * ARRANJO_DY, 4, pecas) for s in (-1, 1)]
saidas += [arranjo(ARRANJO_X, y, 2, pecas) for y in INTERF_Y]

# Cabos coaxiais no chão, das caixas de fase até a parede +X do abrigo.
chegada = Vector((L / 2 + 0.05, 0.0, 0.02))
pecas.append(caixa("Calha de cabos", (L / 2 + 0.3, 0, 0.06), (0.5, 0.6, 0.12), CINZA))
for k, s in enumerate(saidas):
    canto = Vector((ARRANJO_X - 6.5 - 0.15 * k, s.y, 0.02))
    pecas.append(barra("Coaxial", s + Vector((-0.25, 0, 0)), canto, 0.04, PRETO))
    pecas.append(barra("Coaxial", canto, chegada + Vector((0.3, -0.2 + 0.12 * k, 0)), 0.04, PRETO))

fixar(pecas)
radar = juntar("RadarCupri", pecas)
facetar(radar)
paletizar("radar-cupri-paleta", SAIDA.with_name("radar-cupri-paleta.png"))
exportar("radar-cupri", SAIDA)
if PREVIA:
    previa("radar-cupri", PREVIA, camera=(-14.0, -14.0, 9.0), alvo=(6.0, 10.0, 2.0), lente=24)

# Impressora 3D FDM no estilo da Creality Ender-3 (Cena 4 · 2018 SENAI), low poly.
#
#   blender -b -P Ferramentas/blender/impressora_3d.py -- [saida.fbx] [previa.png]
#
# Como a Ender-3 funciona (e por que o modelo é dividido assim):
# - Eixo X: o cabeçote (hotend + ventoinha) corre na barra horizontal, o pórtico.
# - Eixo Y: a mesa aquecida anda para a frente e para trás sobre o trilho central da base.
# - Eixo Z: o pórtico inteiro sobe pelos montantes, puxado por um fuso no lado esquerdo.
# - Extrusora Bowden: o motor no montante esquerdo empurra o filamento do carretel,
#   por um tubo, até o bico quente (~200 °C), que deposita uma camada de cada vez.
# Objetos exportados: Estrutura (fixa), Mesa (Y), Portico (Z) e Cabecote (X).
# Medidas em metros, Z para cima, frente (painel LCD) para -Y. O bico está em
# x = 0, y = -0,035 e toca a mesa (primeira camada) na pose exportada.
import math
import sys
from pathlib import Path

import bpy

sys.path.insert(0, str(Path(__file__).resolve().parent))
from comum import *  # noqa: E402,F403

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
SAIDA = Path(args[0]) if args else RAIZ / "Assets/TrilhaDaInovacao/Modelos/Impressora/impressora-3d.fbx"
PREVIA = Path(args[1]) if len(args) > 1 else None


def caixa(nome, centro, tamanho, mat, giro=(0, 0, 0)):
    """Bloco com cantos levemente chanfrados (loft de retângulo arredondado de 1 segmento)."""
    sx, sy, sz = tamanho
    r = min(sx, sy, 0.03) * 0.12
    ob = loft(nome, [anel_retangular(sx, sy, r, -sz / 2, 0, 1), anel_retangular(sx, sy, r, sz / 2, 0, 1)], mat)
    ob.rotation_euler = giro
    ob.location = centro
    return ob


def cilindro_em(nome, centro, raio, altura, mat, seg=10, giro=(0, 0, 0)):
    ob = cilindro(nome, raio, altura, mat, seg=seg)
    ob.rotation_euler = giro
    ob.location = centro
    return ob


limpar_cena()

PRETO = material("Perfil", (0.07, 0.07, 0.08))
GRAFITE = material("Grafite", (0.16, 0.16, 0.18))
ALUMINIO = material("Aluminio", (0.62, 0.63, 0.66))
SUPERFICIE = material("Superficie", (0.10, 0.10, 0.11))
TELA = material("Tela", (0.22, 0.42, 0.85))
LATAO = material("Latao", (0.78, 0.58, 0.25))
FILAMENTO = material("Filamento", (0.04, 0.72, 0.85))   # PLA ciano, o mesmo do drone
VERMELHO = material("Vermelho", (0.75, 0.12, 0.10))
TUBO = material("TuboPTFE", (0.92, 0.92, 0.90))
CINZA_FONTE = material("Fonte", (0.55, 0.56, 0.58))

X_MONT, Y_MONT = 0.205, 0.035      # montantes (perfis 20x40) à esquerda e à direita
TOPO = 0.465
Y_BICO = -0.035
Z_MESA = 0.076                     # topo da superfície de impressão
Z_BICO = Z_MESA + 0.001
Z_PORTICO = Z_BICO + 0.060         # eixo da barra X (o bico fica 6 cm abaixo dela)

# --- Estrutura fixa -----------------------------------------------------------
est = [
    caixa("Trilho esquerdo", (-0.17, 0.0, 0.02), (0.02, 0.40, 0.04), PRETO),
    caixa("Trilho direito", (0.17, 0.0, 0.02), (0.02, 0.40, 0.04), PRETO),
    caixa("Trilho central (Y)", (0.0, 0.0, 0.02), (0.04, 0.40, 0.04), PRETO),
    caixa("Travessa frontal", (0.0, -0.19, 0.02), (0.36, 0.02, 0.04), PRETO),
    caixa("Travessa traseira", (0.0, 0.19, 0.02), (0.36, 0.02, 0.04), PRETO),
    caixa("Montante esquerdo", (-X_MONT, Y_MONT, 0.04 + (TOPO - 0.04) / 2), (0.02, 0.04, TOPO - 0.04), PRETO),
    caixa("Montante direito", (X_MONT, Y_MONT, 0.04 + (TOPO - 0.04) / 2), (0.02, 0.04, TOPO - 0.04), PRETO),
    caixa("Barra do topo", (0.0, Y_MONT, TOPO - 0.01), (0.43, 0.02, 0.02), PRETO),
    # Placa-mãe sob a base (frente esquerda) e caixa do LCD com botão (frente direita).
    caixa("Caixa da placa", (-0.10, -0.12, 0.012), (0.13, 0.12, 0.024), GRAFITE),
    caixa("Caixa do LCD", (0.165, -0.215, 0.055), (0.12, 0.05, 0.075), GRAFITE, giro=(math.radians(-18), 0, 0)),
    caixa("Tela", (0.152, -0.243, 0.062), (0.065, 0.004, 0.04), TELA, giro=(math.radians(-18), 0, 0)),
    cilindro_em("Botão", (0.200, -0.245, 0.058), 0.009, 0.012, PRETO, seg=8, giro=(math.radians(72), 0, 0)),
    # Fonte de alimentação presa ao montante direito, com a chave vermelha.
    caixa("Fonte", (X_MONT + 0.04, Y_MONT + 0.02, 0.20), (0.05, 0.11, 0.21), CINZA_FONTE),
    caixa("Chave", (X_MONT + 0.066, Y_MONT - 0.01, 0.11), (0.004, 0.015, 0.01), VERMELHO),
    # Eixo Z: motor no pé do montante esquerdo e o fuso até o topo.
    caixa("Motor Z", (-X_MONT + 0.02, Y_MONT - 0.035, 0.06), (0.042, 0.042, 0.04), GRAFITE),
    cilindro_em("Fuso Z", (-X_MONT + 0.02, Y_MONT - 0.035, 0.27), 0.004, 0.38, ALUMINIO, seg=6),
    # Extrusora Bowden no montante esquerdo, perto do topo.
    caixa("Motor da extrusora", (-X_MONT - 0.035, Y_MONT, 0.40), (0.042, 0.042, 0.042), GRAFITE),
    caixa("Alavanca da extrusora", (-X_MONT - 0.035, Y_MONT - 0.026, 0.40), (0.04, 0.01, 0.04), VERMELHO),
    # Carretel de filamento no suporte do topo.
    caixa("Suporte do carretel", (0.0, Y_MONT, TOPO + 0.03), (0.02, 0.02, 0.06), PRETO),
    cilindro_em("Eixo do carretel", (0.0, Y_MONT, TOPO + 0.11), 0.012, 0.09, ALUMINIO, seg=8, giro=(math.radians(90), 0, 0)),
    cilindro_em("Carretel", (0.0, Y_MONT, TOPO + 0.11), 0.10, 0.06, GRAFITE, seg=16, giro=(math.radians(90), 0, 0)),
    cilindro_em("Filamento enrolado", (0.0, Y_MONT, TOPO + 0.11), 0.085, 0.064, FILAMENTO, seg=16, giro=(math.radians(90), 0, 0)),
]
# Filamento do carretel até a extrusora.
est.append(tubo("Filamento", [(-0.07, Y_MONT, TOPO + 0.08), (-0.16, Y_MONT, TOPO), (-X_MONT - 0.035, Y_MONT - 0.01, 0.43)],
                0.0015, FILAMENTO, lados=4))
fixar(est)
estrutura = juntar("Estrutura", est)
facetar(estrutura)

# --- Mesa (eixo Y) ---------------------------------------------------------------
mesa = [
    caixa("Carro da mesa", (0.0, Y_BICO, 0.05), (0.20, 0.20, 0.008), GRAFITE),
    caixa("Mesa aquecida", (0.0, Y_BICO, Z_MESA - 0.0035), (0.235, 0.235, 0.004), ALUMINIO),
    caixa("Superfície de impressão", (0.0, Y_BICO, Z_MESA - 0.0008), (0.225, 0.225, 0.0016), SUPERFICIE),
]
for sx in (-1, 1):
    for sy in (-1, 1):
        mesa.append(cilindro_em("Nivelador", (sx * 0.1, Y_BICO + sy * 0.1, 0.062), 0.016, 0.01, GRAFITE, seg=8))
fixar(mesa)
mesa = juntar("Mesa", mesa)
facetar(mesa)

# --- Pórtico (eixo Z): barra X, placas laterais e motor X ---------------------------
portico = [caixa("Barra X", (0.0, 0.0, Z_PORTICO), (0.40, 0.02, 0.02), PRETO)]
for lado in (-1, 1):
    portico.append(caixa("Placa do pórtico", (lado * X_MONT, Y_MONT / 2, Z_PORTICO), (0.006, 0.07, 0.07), PRETO))
portico.append(caixa("Motor X", (-X_MONT + 0.03, Y_MONT + 0.01, Z_PORTICO), (0.042, 0.042, 0.042), GRAFITE))
fixar(portico)
portico = juntar("Portico", portico)
facetar(portico)

# --- Cabeçote (eixo X): carro, ventoinha, bloco aquecedor e bico de latão --------------
y_frente = -0.0125
cabecote = [
    caixa("Carro X", (0.0, y_frente, Z_PORTICO), (0.06, 0.006, 0.06), GRAFITE),
    caixa("Carenagem", (0.0, Y_BICO, Z_PORTICO - 0.012), (0.045, 0.035, 0.045), PRETO),
    cilindro_em("Ventoinha", (0.0, Y_BICO - 0.018, Z_PORTICO - 0.012), 0.016, 0.003, GRAFITE, seg=8,
                giro=(math.radians(90), 0, 0)),
    caixa("Bloco aquecedor", (0.0, Y_BICO, Z_BICO + 0.012), (0.02, 0.012, 0.01), ALUMINIO),
    tubo("Tubo Bowden", [(0.0, Y_BICO, Z_PORTICO + 0.01), (0.0, Y_BICO, Z_PORTICO + 0.07), (-0.06, Y_BICO + 0.03, Z_PORTICO + 0.17)],
         0.002, TUBO, lados=4),
]
bico = torno("Bico", [(0.0, 0.0), (0.0015, 0.0), (0.005, 0.005), (0.005, 0.007), (0.0, 0.007)], LATAO, seg=6)
bico.location = (0.0, Y_BICO, Z_BICO)
cabecote.append(bico)
fixar(cabecote)
cabecote = juntar("Cabecote", cabecote)
facetar(cabecote)

paletizar("impressora-3d-paleta", SAIDA.with_name("impressora-3d-paleta.png"))
exportar("impressora", SAIDA)
if PREVIA:
    previa("impressora", PREVIA, camera=(0.55, -0.95, 0.55), alvo=(0.0, 0.0, 0.24), lente=40)

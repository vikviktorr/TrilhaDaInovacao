# Console do operador do radar CUPRI para a Cena 2 · 1994 (Campanha Guará), low poly.
#
#   blender -b -P Ferramentas/blender/console_radar.py -- [saida.fbx] [previa.png]
#
# O CUPRI (radar_cupri.py) não gira: as antenas olham para o zênite. Por isso o
# console não tem a tela PPI redonda dos radares de vigilância. O operador vê os
# ecos num monitor de computador, como um mapa RTI (altura × hora, com a cor
# dando a força do eco), onde aparecem a faixa do eletrojato e as plumas das
# bolhas de plasma do spread F (Swartz e Woodman, GRL 1998). Móvel de aço cinza
# com mesa e painel inclinado; no painel, o monitor RTI, o osciloscópio que
# confere o pulso transmitido e o eco (A-scope), medidores de potência,
# onda refletida e corrente do transmissor, a fileira de lâmpadas de estado e os
# botões de ajuste. Na mesa, o teclado, chaves de alavanca e a caixa de comando
# com o botão grande de ligar o transmissor, de aro amarelo e preto de segurança
# (o botão em si é criado no Unity, para brilhar e aceitar o toque). A caixa
# deixa o botão a 0,9 m, na faixa confortável de toque em VR.
#
# Medidas em metros, Z para cima, frente (o lado do operador) para -Y. Cotas que
# o construtor de cenas usa (no Unity, com giro de 180°: x, z, y do Blender):
#   painel inclinado: de (y 0,02; z 0,78) a (y 0,25; z 1,50)
#   monitor RTI: u = -0,27, s = 0,55, tela de 0,40 × 0,30   (u ao longo de X, s de 0 a 1 subindo o painel)
#   tela A-scope: u = 0,22, s = 0,62, 0,28 × 0,16
#   botão de ligar: x 0,42, y -0,24, sobre a caixa de comando (z 0,9)
import math
import sys
from pathlib import Path

import bmesh
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from comum import *  # noqa: E402,F403

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
SAIDA = Path(args[0]) if args else RAIZ / "Assets/TrilhaDaInovacao/Modelos/Guara/console-radar.fbx"
PREVIA = Path(args[1]) if len(args) > 1 else None

limpar_cena()
CINZA = material("Cinza", (0.6, 0.62, 0.58))
CINZA_ESCURO = material("CinzaEscuro", (0.22, 0.23, 0.24))
PRETO = material("Preto", (0.05, 0.05, 0.06))
TELA = material("Tela", (0.02, 0.09, 0.05))
BRANCO = material("Branco", (0.9, 0.9, 0.86))
VERMELHO = material("Vermelho", (0.8, 0.12, 0.1))
AMBAR = material("Ambar", (0.95, 0.6, 0.1))
VERDE_LAMPADA = material("VerdeLampada", (0.2, 0.75, 0.3))
AMARELO = material("Amarelo", (0.95, 0.8, 0.1))
BEGE = material("Bege", (0.78, 0.74, 0.62))

# Painel inclinado: base (y0, z0) até topo (y1, z1).
Y0, Z0, Y1, Z1 = 0.02, 0.78, 0.25, 1.50
COMP = math.hypot(Y1 - Y0, Z1 - Z0)
NORMAL = Vector((0, -(Z1 - Z0), Y1 - Y0)).normalized()   # para a frente e para cima


def no_painel(u, s, saliencia=0.0):
    """Ponto no painel inclinado (u ao longo de X, s de 0 a 1 subindo)."""
    return Vector((u, Y0 + (Y1 - Y0) * s, Z0 + (Z1 - Z0) * s)) + NORMAL * saliencia


def caixa(nome, x0, x1, y0, y1, z0, z1, mat):
    ob = loft(nome, [anel_retangular(x1 - x0, y1 - y0, 0.004, z0, 0, 1),
                     anel_retangular(x1 - x0, y1 - y0, 0.004, z1, 0, 1)], mat)
    ob.location = ((x0 + x1) / 2, (y0 + y1) / 2, 0)
    return ob


def prisma(nome, perfil_yz, x0, x1, mat):
    """Sólido com o perfil (y, z) dado, estendido de x0 a x1."""
    bm = bmesh.new()
    a = [bm.verts.new((x0, y, z)) for y, z in perfil_yz]
    b = [bm.verts.new((x1, y, z)) for y, z in perfil_yz]
    bm.faces.new(a[::-1])
    bm.faces.new(b)
    n = len(perfil_yz)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((a[i], a[j], b[j], b[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return objeto_de_bmesh(nome, bm, mat)


def no_plano(nome, centro, ob):
    """Deita uma peça feita no plano XY (frente +Z) sobre o painel inclinado."""
    ob.rotation_euler = NORMAL.to_track_quat("Z", "Y").to_euler()
    ob.location = centro
    return ob


def disco(nome, raio, altura, mat, seg=20):
    return cilindro(nome, raio, altura, mat, seg=seg, centro=(0, 0, altura / 2))


def anel(nome, r_int, r_ext, altura, mat, seg=28):
    return torno(nome, [(r_int, altura), (r_ext, altura), (r_ext, 0.0), (r_int, 0.0)], mat, seg=seg)


pecas = []
# Móvel: gabinete, mesa com borda e painel inclinado com tampa.
pecas.append(caixa("Gabinete", -0.6, 0.6, -0.12, 0.35, 0.0, 0.74, CINZA))
pecas.append(caixa("Rodape", -0.58, 0.58, -0.1, 0.33, 0.0, 0.06, CINZA_ESCURO))
pecas.append(caixa("Mesa", -0.62, 0.62, -0.35, 0.35, 0.74, 0.78, CINZA_ESCURO))
pecas.append(prisma("Painel", [(Y0, Z0), (0.35, Z0), (0.35, Z1), (Y1, Z1)], -0.6, 0.6, CINZA))
pecas.append(caixa("Tampa", -0.62, 0.62, 0.2, 0.37, Z1, Z1 + 0.04, CINZA_ESCURO))
for x in (-0.62, 0.58):
    pecas.append(prisma(f"Lateral {x}", [(Y0 - 0.01, Z0), (0.36, Z0), (0.36, Z1 + 0.01), (Y1 - 0.01, Z1 + 0.01)], x, x + 0.04, CINZA_ESCURO))

# Monitor RTI: moldura bege do monitor, vidro escuro e os botões de ajuste abaixo.
c = no_painel(-0.27, 0.55)
moldura = loft("Moldura RTI", [anel_retangular(0.46, 0.36, 0.02, 0.0, 0, 2), anel_retangular(0.46, 0.36, 0.02, 0.022, 0, 2)], BEGE)
pecas.append(no_plano("Moldura RTI", c, moldura))
tela_rti = loft("Tela RTI", [anel_retangular(0.4, 0.3, 0.012, 0.0, 0, 2), anel_retangular(0.4, 0.3, 0.012, 0.026, 0, 2)], TELA)
pecas.append(no_plano("Tela RTI", c, tela_rti))
for k, u in enumerate((-0.38, -0.27, -0.16)):
    pecas.append(no_plano(f"Botao RTI {k}", no_painel(u, 0.15), disco(f"Botao RTI {k}", 0.022, 0.025, PRETO, seg=10)))

# A-scope: moldura retangular e tela.
c = no_painel(0.22, 0.62)
moldura = loft("Moldura A", [anel_retangular(0.32, 0.2, 0.01, 0.0, 0, 1), anel_retangular(0.32, 0.2, 0.01, 0.02, 0, 1)], PRETO)
pecas.append(no_plano("Moldura A", c, moldura))
tela_a = loft("Tela A", [anel_retangular(0.28, 0.16, 0.01, 0.0, 0, 1), anel_retangular(0.28, 0.16, 0.01, 0.024, 0, 1)], TELA)
pecas.append(no_plano("Tela A", c, tela_a))

# Medidores analógicos (potência, onda refletida, corrente do transmissor) e as lâmpadas de estado.
for k, u in enumerate((0.1, 0.22, 0.34)):
    p = no_painel(u, 0.27)
    pecas.append(no_plano(f"Aro medidor {k}", p, anel(f"Aro medidor {k}", 0.04, 0.05, 0.02, PRETO, seg=16)))
    pecas.append(no_plano(f"Medidor {k}", p, disco(f"Medidor {k}", 0.041, 0.012, BRANCO, seg=16)))
    ponteiro = loft(f"Ponteiro {k}", [anel_retangular(0.004, 0.035, 0, 0.0, 0, 1), anel_retangular(0.004, 0.035, 0, 0.016, 0, 1)], PRETO)
    ponteiro.location.y = 0.012
    ponteiro.rotation_euler.z = (k - 1) * 0.6
    fixar([ponteiro])
    pecas.append(no_plano(f"Ponteiro {k}", p, ponteiro))
cores = [VERDE_LAMPADA, VERDE_LAMPADA, AMBAR, VERDE_LAMPADA, VERMELHO, AMBAR, VERDE_LAMPADA, VERMELHO, VERDE_LAMPADA, AMBAR]
for k, mat in enumerate(cores):
    pecas.append(no_plano(f"Lampada {k}", no_painel(-0.5 + k * 0.11, 0.9), disco(f"Lampada {k}", 0.017, 0.02, mat, seg=10)))

# Mesa: teclado, chaves de alavanca e o aro do botão de ligar.
pecas.append(caixa("Teclado", -0.56, -0.16, -0.32, -0.16, 0.78, 0.8, BEGE))
for k in range(30):
    x = -0.54 + (k % 10) * 0.037
    y = -0.3 + (k // 10) * 0.045
    pecas.append(caixa(f"Tecla {k}", x, x + 0.03, y, y + 0.036, 0.8, 0.812, BRANCO if k % 10 else AMBAR))
for k in range(4):
    x = -0.1 + k * 0.08
    pecas.append(caixa(f"Base chave {k}", x - 0.02, x + 0.02, -0.24, -0.2, 0.78, 0.79, PRETO))
    alavanca = cilindro(f"Alavanca {k}", 0.006, 0.05, BRANCO, seg=6, centro=(0, 0, 0.025))
    alavanca.rotation_euler.x = -0.4
    alavanca.location = (x, -0.22, 0.79)
    pecas.append(alavanca)
BX, BY, BZ = 0.42, -0.24, 0.9
pecas.append(caixa("Caixa de comando", BX - 0.11, BX + 0.11, BY - 0.1, BY + 0.1, 0.78, BZ, CINZA_ESCURO))
pecas.append(anel("Aro de seguranca", 0.075, 0.1, 0.012, AMARELO, seg=20))
pecas[-1].location = (BX, BY, BZ)
for k in range(4):
    faixa = loft(f"Faixa {k}", [anel_retangular(0.03, 0.022, 0, 0.0, 0, 1), anel_retangular(0.03, 0.022, 0, 0.014, 0, 1)], PRETO)
    a = k * math.pi / 2 + math.pi / 4
    faixa.rotation_euler.z = a
    faixa.location = (BX + 0.088 * math.cos(a), BY + 0.088 * math.sin(a), BZ)
    pecas.append(faixa)

fixar(pecas)
console = juntar("ConsoleRadar", pecas)
facetar(console)
paletizar("console-radar-paleta", SAIDA.with_name("console-radar-paleta.png"))
exportar("console-radar", SAIDA)
if PREVIA:
    previa("console-radar", PREVIA, camera=(0.9, -1.9, 1.7), alvo=(0.0, 0.0, 0.95), lente=40)

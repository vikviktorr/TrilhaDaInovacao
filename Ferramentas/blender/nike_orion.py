# Foguete de sondagem Nike-Orion no lançador de trilho, low poly, para a Cena 2 ·
# 1994 (Campanha Guará, Alcântara). Na campanha, de agosto a outubro de 1994, a
# NASA lançou de Alcântara foguetes Nike-Orion (quatro voos em agosto, até ~140 km),
# Nike-Tomahawk e Black Brant para estudar a ionosfera no equador magnético.
#
#   blender -b -P Ferramentas/blender/nike_orion.py -- [saida.fbx] [previa.png]
#
# Nike-Orion: dois estágios de propelente sólido. 1º estágio (booster Nike) de
# 0,42 m de diâmetro com quatro aletas grandes; 2º estágio (Orion) de 0,36 m com
# quatro aletas pequenas e a carga útil com coifa ogival; ~10 m no total.
# Lançador: base giratória sobre pés, braço de elevação e trilho a 82° (o foguete
# sobe quase na vertical, inclinado para o mar). Z para cima, frente para -Y.
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from comum import *  # noqa: E402,F403

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
SAIDA = Path(args[0]) if args else RAIZ / "Assets/TrilhaDaInovacao/Modelos/Guara/nike-orion-lancador.fbx"
PREVIA = Path(args[1]) if len(args) > 1 else None

ELEVACAO = math.radians(82)
limpar_cena()
BRANCO = material("Branco", (0.92, 0.92, 0.9))
PRETO = material("Preto", (0.08, 0.08, 0.09))
LARANJA = material("Laranja", (0.9, 0.42, 0.1))
AÇO = material("Aco", (0.45, 0.47, 0.5), metal=0.6, rugosidade=0.5)
AMARELO = material("Amarelo", (0.86, 0.68, 0.12))
CONCRETO = material("Concreto", (0.62, 0.6, 0.56))


def aleta(nome, raio, z0, raiz, ponta, envergadura, giro, mat, espessura=0.03):
    """Aleta trapezoidal no plano XZ, presa ao corpo no raio dado."""
    import bmesh
    bm = bmesh.new()
    pts = [(raio, z0), (raio, z0 + raiz), (raio + envergadura, z0 + ponta * 0.6 + 0.05), (raio + envergadura, z0)]
    frente = [bm.verts.new((x, -espessura / 2, z)) for x, z in pts]
    tras = [bm.verts.new((x, espessura / 2, z)) for x, z in pts]
    bm.faces.new(frente[::-1])
    bm.faces.new(tras)
    for i in range(4):
        j = (i + 1) % 4
        bm.faces.new((frente[i], frente[j], tras[j], tras[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    ob = objeto_de_bmesh(nome, bm, mat)
    ob.rotation_euler = (0, 0, giro)
    return ob


def foguete():
    pecas = []
    # 1º estágio: booster Nike, 3,4 m, aletas grandes e tubeira.
    pecas.append(torno("Tubeira Nike", [(0.0, 0.25), (0.17, 0.25), (0.2, 0.0), (0.0, 0.0)], PRETO, seg=12))
    pecas.append(torno("Nike", [(0.0, 3.6), (0.21, 3.6), (0.21, 0.25), (0.0, 0.25)], BRANCO, seg=12))
    pecas.append(torno("Faixa Nike", [(0.0, 2.2), (0.215, 2.2), (0.215, 1.9), (0.0, 1.9)], PRETO, seg=12))
    for k in range(4):
        pecas.append(aleta(f"Aleta Nike {k}", 0.2, 0.3, 1.0, 0.6, 0.62, k * math.pi / 2 + math.pi / 4, LARANJA))
    # Interestágio cônico.
    pecas.append(torno("Interestagio", [(0.0, 3.9), (0.18, 3.9), (0.21, 3.6), (0.0, 3.6)], AÇO, seg=12))
    # 2º estágio: Orion, com aletas pequenas.
    pecas.append(torno("Orion", [(0.0, 6.6), (0.18, 6.6), (0.18, 3.9), (0.0, 3.9)], BRANCO, seg=12))
    pecas.append(torno("Faixa Orion", [(0.0, 5.4), (0.185, 5.4), (0.185, 5.15), (0.0, 5.15)], PRETO, seg=12))
    for k in range(4):
        pecas.append(aleta(f"Aleta Orion {k}", 0.17, 3.95, 0.55, 0.35, 0.3, k * math.pi / 2 + math.pi / 4, LARANJA, 0.02))
    # Carga útil (instrumentos de plasma) e coifa ogival.
    pecas.append(torno("Carga util", [(0.0, 8.3), (0.18, 8.3), (0.18, 6.6), (0.0, 6.6)], AÇO, seg=12))
    pecas.append(torno("Coifa", [(0.0, 10.0), (0.05, 9.85), (0.12, 9.3), (0.17, 8.7), (0.18, 8.3), (0.0, 8.3)], BRANCO, seg=12))
    return pecas


def lancador():
    pecas = []
    # Laje, base giratória e pés.
    pecas.append(loft("Laje", [anel_retangular(4.5, 4.5, 0.1, 0.0, 0, 1), anel_retangular(4.5, 4.5, 0.1, 0.2, 0, 1)], CONCRETO))
    pecas.append(cilindro("Base giratoria", 0.9, 0.5, AMARELO, seg=14, centro=(0, 0, 0.45)))
    for k in range(4):
        a = k * math.pi / 2 + math.pi / 4
        pecas.append(tubo(f"Pe {k}", [(0, 0, 0.6), (1.6 * math.cos(a), 1.6 * math.sin(a), 0.2)], 0.09, AMARELO, lados=6))
    # Munhão (eixo de elevação) e o braço do trilho, inclinado para +Y.
    pecas.append(loft("Munhao", [anel_retangular(0.6, 0.6, 0.05, 0.7, 0, 1), anel_retangular(0.6, 0.6, 0.05, 1.6, 0, 1)], AMARELO))
    return pecas


def inclinar(obs, pivo, angulo):
    """Gira as peças em torno do eixo X que passa por pivo (elevação do trilho)."""
    import bpy
    from mathutils import Matrix, Vector
    bpy.context.view_layer.update()   # matrix_world ainda não viu as mudanças de location
    m = Matrix.Translation(Vector(pivo)) @ Matrix.Rotation(angulo, 4, "X") @ Matrix.Translation(-Vector(pivo))
    for ob in obs:
        ob.matrix_world = m @ ob.matrix_world


base = lancador()

# Trilho: viga de 9 m apoiada no munhão, com o foguete pendurado de lado.
PIVO = (0.0, 0.0, 1.5)
trilho = [loft("Trilho", [anel_retangular(0.22, 0.3, 0.02, 1.2, 0, 1), anel_retangular(0.22, 0.3, 0.02, 11.0, 0, 1)], AÇO)]
trilho.append(loft("Escora", [anel_retangular(0.14, 0.14, 0.02, 1.5, 0, 1), anel_retangular(0.14, 0.14, 0.02, 5.0, 0, 1)], AMARELO))
trilho[1].location = (0, 0.55, 0)
trilho[1].rotation_euler = (math.radians(-6), 0, 0)
corpo = foguete()
for ob in corpo:
    ob.location.y -= 0.42   # ao lado do trilho (sapatas deslizando nele)
    ob.location.z += 1.85
for k, z in enumerate((2.6, 6.6)):
    trilho.append(loft(f"Sapata {k}", [anel_retangular(0.12, 0.24, 0.0, z, 0, 1), anel_retangular(0.12, 0.24, 0.0, z + 0.2, 0, 1)], AÇO))
    trilho[-1].location.y = -0.24
inclinar(trilho + corpo, PIVO, -(math.pi / 2 - ELEVACAO))

pecas = base + trilho + corpo
fixar(pecas)
modelo = juntar("NikeOrion", pecas)
facetar(modelo)
paletizar("nike-orion-paleta", SAIDA.with_name("nike-orion-paleta.png"))
exportar("nike-orion", SAIDA)
if PREVIA:
    previa("nike-orion", PREVIA, camera=(9.0, -14.0, 6.0), alvo=(0.0, 0.5, 5.0), lente=35)

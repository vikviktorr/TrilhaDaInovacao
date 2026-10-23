# Algarismos 0 a 9 em 3D para o ano que aparece no túnel do tempo (TunelDoTempo).
# Letra grossa extrudada e chanfrada, com a luz "pintada" na paleta: frente dourada,
# chanfros de cima quase brancos e laterais de baixo em laranja e vinho, como os
# títulos cromados dos filmes de viagem no tempo. No Unity o material é Unlit, então
# o ano tem o mesmo volume em qualquer época, sem depender da luz da cena.
#
#   blender -b -P Ferramentas/blender/digitos_3d.py -- [saida.fbx] [previa.png]
#
# Medidas em metros, Z para cima, frente para -Y. O "0" tem 1 m de altura, cada
# algarismo tem o centro horizontal em x = 0 e a linha de base em z = 0.
# Objetos exportados: Digito0 ... Digito9, com um material só (textura-paleta).
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from comum import *  # noqa: E402,F403

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
SAIDA = Path(args[0]) if args else RAIZ / "Assets/TrilhaDaInovacao/Modelos/Ano3D/digitos-3d.fbx"
PREVIA = Path(args[1]) if len(args) > 1 else None

# Noto Sans Condensed Black (OFL). Sem ela, a fonte embutida do Blender.
FONTES = [
    "/usr/share/fonts/google-noto/NotoSans-CondensedBlack.ttf",
    "/usr/share/fonts/truetype/noto/NotoSans-CondensedBlack.ttf",
    "/usr/share/fonts/google-noto/NotoSans-Black.ttf",
    "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
]

# Rampa de luz, do lado na sombra ao chanfro de cima.
RAMPA = [
    (0.20, 0.02, 0.03),   # vinho
    (0.45, 0.05, 0.02),
    (0.80, 0.18, 0.02),   # laranja
    (1.00, 0.42, 0.05),
    (1.00, 0.66, 0.16),   # dourado
    (1.00, 0.84, 0.42),
    (1.00, 0.96, 0.80),   # quase branco
]
# Luz de cima, da esquerda e da frente (no espaço do texto: +Z é a frente).
LUZ = Vector((-0.35, 0.75, 0.55)).normalized()

limpar_cena()
fonte = next((bpy.data.fonts.load(f) for f in FONTES if Path(f).exists()), None)
materiais = [material(f"Luz{k}", c, rugosidade=0.6) for k, c in enumerate(RAMPA)]


def algarismo(d):
    curva = bpy.data.curves.new(f"Texto{d}", "FONT")
    curva.body = str(d)
    if fonte:
        curva.font = fonte
    curva.size = 1.0
    curva.align_x = "CENTER"
    curva.resolution_u = 3          # low poly: curvas com poucos segmentos
    curva.extrude = 0.09
    curva.bevel_depth = 0.03
    curva.bevel_resolution = 1
    ob = bpy.data.objects.new(f"Digito{d}", curva)
    bpy.context.collection.objects.link(ob)

    for o in bpy.context.selected_objects:
        o.select_set(False)
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.convert(target="MESH")
    ob = bpy.context.view_layer.objects.active
    ob.name = ob.data.name = f"Digito{d}"

    # Cada face recebe a cor da rampa conforme a luz que bateria nela.
    for m in materiais:
        ob.data.materials.append(m)
    for poly in ob.data.polygons:
        k = (poly.normal.dot(LUZ) + 1.0) / 2.0
        poly.material_index = min(len(RAMPA) - 1, int(k * len(RAMPA)))
    return ob


digitos = [algarismo(d) for d in range(10)]

# Altura do "0" = 1 m; texto deitado no plano XY vira de pé, de frente para -Y.
caixa = [Vector(c) for c in digitos[0].bound_box]
escala = 1.0 / (max(c.y for c in caixa) - min(c.y for c in caixa))
for ob in digitos:
    ob.scale = (escala, escala, escala)
    ob.rotation_euler = (math.radians(90), 0, 0)
    aplicar_transformacoes(ob)
    pts = [Vector(c) for c in ob.bound_box]
    centro = (max(p.x for p in pts) + min(p.x for p in pts)) / 2
    for v in ob.data.vertices:
        v.co.x -= centro
    facetar(ob)

# ----------------------------------------------------------------------------
# Exportação
# ----------------------------------------------------------------------------
paletizar("digitos-3d-paleta", SAIDA.with_name("digitos-3d-paleta.png"))
exportar("dígitos 3D", SAIDA)
if PREVIA:
    for k, ob in enumerate(digitos):
        ob.location.x = (k - 4.5) * 0.62
    previa("dígitos 3D", PREVIA, camera=(-1.2, -7.5, 2.2), alvo=(0, 0, 0.5))

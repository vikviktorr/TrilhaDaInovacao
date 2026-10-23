# Telefone de mesa de baquelite com disco rotativo (Cena 1 · 1986 TELMA).
# Modelado a partir de foto de referência, em low poly (como os modelos da Kenney);
# gera o FBX usado pelo construtor de cenas.
#
#   blender -b -P Ferramentas/blender/telefone_1986.py -- [saida.fbx] [previa.png]
#
# Medidas em metros, Z para cima, frente (disco) para -Y.
# Objetos exportados: Corpo (base + gancho: é o alvo do brilho no Unity) e
# Detalhes (disco, fone e fio). Um material só, com textura-paleta.
import math
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from comum import *  # noqa: E402,F403

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
SAIDA = Path(args[0]) if args else RAIZ / "Assets/TrilhaDaInovacao/Modelos/Telefone/telefone-1986.fbx"
PREVIA = Path(args[1]) if len(args) > 1 else None

# ----------------------------------------------------------------------------
# Modelo
# ----------------------------------------------------------------------------
limpar_cena()

# Preto puro esconde as facetas: um grafite escuro deixa o flat aparecer.
BAQUELITE = material("Baquelite", (0.09, 0.09, 0.10))
MARFIM = material("Marfim", (0.86, 0.80, 0.64))
ROTULO = material("Rotulo", (0.55, 0.72, 0.82))
METAL = material("Metal", (0.62, 0.60, 0.56))

# --- Corpo: base em degraus + corpo que afina, com a frente inclinada --------
# (z, largura, profundidade, raio do canto, deslocamento em y do centro)
perfil_corpo = [
    (0.000, 0.205, 0.215, 0.026, 0.000),
    (0.011, 0.205, 0.215, 0.026, 0.000),
    (0.011, 0.199, 0.209, 0.024, 0.000),   # friso
    (0.015, 0.199, 0.209, 0.024, 0.000),
    (0.015, 0.203, 0.213, 0.025, 0.000),
    (0.026, 0.203, 0.213, 0.025, 0.000),
    (0.026, 0.195, 0.205, 0.023, 0.000),
    (0.031, 0.190, 0.200, 0.023, 0.000),
    (0.052, 0.164, 0.178, 0.026, 0.007),
    (0.090, 0.151, 0.140, 0.028, 0.022),
    (0.110, 0.147, 0.115, 0.028, 0.032),
    (0.117, 0.143, 0.106, 0.026, 0.036),
    (0.120, 0.136, 0.099, 0.022, 0.036),
]
corpo = loft("Corpo", [anel_retangular(w, d, r, z, cy) for z, w, d, r, cy in perfil_corpo], BAQUELITE)

# Pescoço do gancho, atrás no topo.
CY_GANCHO = 0.045
pescoco = loft("Pescoco", [
    anel_retangular(0.118, 0.050, 0.014, 0.112, CY_GANCHO),
    anel_retangular(0.118, 0.050, 0.014, 0.134, CY_GANCHO),
    anel_retangular(0.112, 0.046, 0.012, 0.138, CY_GANCHO),
], BAQUELITE)

# Garfos do gancho: um bloco com dois pinos arredondados de cada lado.
Z_APOIO = 0.146
partes_gancho = [corpo, pescoco]
for lado in (-1, 1):
    x = lado * 0.047
    partes_gancho.append(loft(f"Garfo{lado}", [
        anel_retangular(0.020, 0.054, 0.008, 0.130, CY_GANCHO),
        anel_retangular(0.020, 0.054, 0.008, Z_APOIO, CY_GANCHO),
    ], BAQUELITE))
    partes_gancho[-1].location.x = x
    for dy in (-0.019, 0.019):
        pino = torno(f"Pino{lado}{dy}", [
            (0.0, 0.182), (0.0075, 0.178), (0.0095, 0.172), (0.0095, 0.140),
        ], BAQUELITE, seg=6)
        pino.location = (x, CY_GANCHO + dy, 0)
        partes_gancho.append(pino)
for o in partes_gancho:
    aplicar_transformacoes(o)
    for o2 in [o]:
        bpy.context.view_layer.objects.active = o2
        o2.select_set(True)
        bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)
        o2.select_set(False)
corpo = juntar("Corpo", partes_gancho)
facetar(corpo)

# --- Disco rotativo na frente inclinada -------------------------------------
# Plano da frente entre os anéis z=0.052 e z=0.110 (centro do anel - prof/2).
frente_a = Vector((0, 0.007 - 0.178 / 2, 0.052))
frente_b = Vector((0, 0.032 - 0.115 / 2, 0.110))
tangente = (frente_b - frente_a).normalized()
normal = Vector((0, -tangente.z, tangente.y)).normalized()
inclinacao = math.atan2(-normal.y, normal.z)            # giro em X que leva +Z à normal
centro_disco = frente_a.lerp(frente_b, 0.45)

R_DISCO = 0.052
partes_disco = []

# Assento do disco (preto), embutido no corpo.
assento = cilindro("Assento", R_DISCO + 0.004, 0.018, BAQUELITE, seg=16, centro=(0, 0, -0.004))
partes_disco.append(assento)

# Placa de marfim sob a roda: aparece pelos furos (sem números, como nos modelos Kenney).
placa = cilindro("Placa", R_DISCO - 0.002, 0.002, MARFIM, seg=16, centro=(0, 0, 0.0055))
partes_disco.append(placa)

ANGULOS = [60 + 27 * k for k in range(10)]     # 1, 2, ..., 9, 0 (anti-horário)
R_FUROS = 0.0395
R_FURO = 0.0078
# Roda de discar (preta) com dez furos.
roda = cilindro("Roda", R_DISCO, 0.004, BAQUELITE, seg=20, centro=(0, 0, 0.0092))
furos = []
for ang in ANGULOS:
    a = math.radians(ang)
    furos.append(cilindro("Furo", R_FURO, 0.02, BAQUELITE, seg=6,
                          centro=(R_FUROS * math.cos(a), R_FUROS * math.sin(a), 0.0092)))
corte = juntar("Corte", furos)
bool_mod = roda.modifiers.new("Furos", "BOOLEAN")
bool_mod.operation = "DIFFERENCE"
bool_mod.object = corte
bool_mod.solver = "EXACT"
bpy.context.view_layer.objects.active = roda
bpy.ops.object.modifier_apply(modifier=bool_mod.name)
bpy.data.objects.remove(corte)
partes_disco.append(roda)

# Etiqueta central azul com aro.
aro = cilindro("Aro", 0.0215, 0.003, BAQUELITE, seg=10, centro=(0, 0, 0.0122))
etiqueta = cilindro("Etiqueta", 0.018, 0.0008, ROTULO, seg=10, centro=(0, 0, 0.0138))
partes_disco += [aro, etiqueta]

# Batente de metal (o "dedo" que para a roda), às 4 horas.
batente = bpy.data.meshes.new("Batente")
bm = bmesh.new()
bmesh.ops.create_cube(bm, size=1.0)
bm.to_mesh(batente)
bm.free()
ob_batente = bpy.data.objects.new("Batente", batente)
bpy.context.collection.objects.link(ob_batente)
ob_batente.data.materials.append(METAL)
a = math.radians(-38)
ob_batente.location = ((R_DISCO + 0.001) * math.cos(a), (R_DISCO + 0.001) * math.sin(a), 0.0125)
ob_batente.rotation_euler = (0, 0, a + math.radians(-60))
ob_batente.scale = (0.018, 0.006, 0.004)
partes_disco.append(ob_batente)

for o in partes_disco:
    o.select_set(False)
for o in partes_disco:
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    o.select_set(False)
disco = juntar("Disco", partes_disco)
disco.matrix_world = Matrix.Translation(centro_disco) @ Matrix.Rotation(inclinacao, 4, "X")
aplicar_transformacoes(disco)
bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)
facetar(disco)

# --- Fone: alça arqueada + auscultador e bocal ------------------------------
Z_ALCA = Z_APOIO + 0.0135
alca_pts = catmull_rom([
    (-0.100, CY_GANCHO, Z_ALCA - 0.004),
    (-0.060, CY_GANCHO, Z_ALCA + 0.002),
    (0.000, CY_GANCHO, Z_ALCA + 0.005),
    (0.060, CY_GANCHO, Z_ALCA + 0.002),
    (0.100, CY_GANCHO, Z_ALCA - 0.004),
], 2)
alca = tubo("Alca", [tuple(p) for p in alca_pts], 0.0145, BAQUELITE, lados=8)

perfil_concha = [
    (0.000, 0.054), (0.020, 0.050), (0.031, 0.029), (0.0375, 0.004), (0.0375, 0.000),
    (0.000, 0.000),
]
partes_fone = [alca]
for lado in (-1, 1):
    concha = torno(f"Concha{lado}", perfil_concha, BAQUELITE, seg=8)
    concha.rotation_euler = (0, math.radians(lado * 14), 0)
    concha.location = (lado * 0.112, CY_GANCHO, Z_ALCA - 0.056)
    partes_fone.append(concha)
for o in partes_fone:
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    o.select_set(False)
fone = juntar("Fone", partes_fone)
facetar(fone)

# --- Fio espiralado: do bocal (direita) descendo até a lateral da base -------
espinha = catmull_rom([
    (0.135, CY_GANCHO + 0.012, Z_ALCA - 0.050),
    (0.165, 0.080, 0.090),
    (0.180, 0.050, 0.055),
    (0.160, 0.000, 0.035),
    (0.118, -0.020, 0.030),
    (0.100, 0.030, 0.018),
], 6)
espiral = []
voltas, por_volta, r_espiral = 16, 6, 0.0075
total = voltas * por_volta
comprimentos = [0.0]
for a_, b_ in zip(espinha, espinha[1:]):
    comprimentos.append(comprimentos[-1] + (b_ - a_).length)
L = comprimentos[-1]
u = None
for i in range(total + 1):
    s = L * i / total
    k = max(j for j, c in enumerate(comprimentos) if c <= s)
    k = min(k, len(espinha) - 2)
    t = (s - comprimentos[k]) / max(1e-9, comprimentos[k + 1] - comprimentos[k])
    p = espinha[k].lerp(espinha[k + 1], t)
    tang = (espinha[k + 1] - espinha[k]).normalized()
    # Referencial transportado ao longo da espinha: a espiral não torce.
    u = tang.cross(Vector((0, 0, 1))) if u is None else u - u.dot(tang) * tang
    u.normalize()
    v = tang.cross(u)
    ang = 2 * math.pi * i / por_volta
    espiral.append(tuple(p + r_espiral * (math.cos(ang) * u + math.sin(ang) * v)))
fio = tubo("Fio", espiral, 0.0028, BAQUELITE, lados=4)
facetar(fio)

# ----------------------------------------------------------------------------
# Exportação
# ----------------------------------------------------------------------------
juntar("Detalhes", [disco, fone, fio])
paletizar("telefone-1986-paleta", SAIDA.with_name("telefone-1986-paleta.png"))
exportar("telefone", SAIDA)
if PREVIA:
    previa("telefone", PREVIA, camera=(0.0, -0.62, 0.36), alvo=(0, 0.01, 0.085))

# Rádio de mesa retrô (madeira escura, moldura de cobre, grade de ripas e antena),
# modelado a partir de foto de referência em low poly (como os modelos da Kenney).
# Fica no balcão da Cena 1 · 1986 TELMA.
#
#   blender -b -P Ferramentas/blender/radio_retro.py -- [saida.fbx] [previa.png]
#
# Medidas em metros, Z para cima, frente (dial) para -Y. Objetos exportados:
# Gabinete, Frente (moldura, grade e painel), Controles e Antena, com um material
# só (textura-paleta).
import math
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from comum import *  # noqa: E402,F403

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
SAIDA = Path(args[0]) if args else RAIZ / "Assets/TrilhaDaInovacao/Modelos/Radio/radio-retro.fbx"
PREVIA = Path(args[1]) if len(args) > 1 else None

LARG, ALT = 0.26, 0.175
Y_FRENTE, Y_FUNDO = -0.050, 0.0625      # face frontal do gabinete (a moldura fica à frente)


def anel_xz(larg, alt, raio, y, cz, seg=2):
    """Retângulo arredondado no plano XZ (visto de frente), na profundidade y."""
    # anel_retangular desenha no plano XY: o "y" dele vira a altura aqui.
    return [(x, y, cz + z) for x, z, _ in anel_retangular(larg, alt, raio, 0, 0, seg)]


def moldura(nome, externo, interno, y0, y1, cz, mat, seg=2):
    """Aro com furo: (largura, altura, raio) externo e interno, de y0 (frente) a y1."""
    bm = bmesh.new()
    ef = [bm.verts.new(p) for p in anel_xz(*externo, y0, cz, seg)]
    et = [bm.verts.new(p) for p in anel_xz(*externo, y1, cz, seg)]
    if_ = [bm.verts.new(p) for p in anel_xz(*interno, y0, cz, seg)]
    it = [bm.verts.new(p) for p in anel_xz(*interno, y1, cz, seg)]
    n = len(ef)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((ef[i], ef[j], et[j], et[i]))       # lado de fora
        bm.faces.new((it[i], it[j], if_[j], if_[i]))     # lado de dentro
        bm.faces.new((if_[i], if_[j], ef[j], ef[i]))     # frente
        bm.faces.new((et[i], et[j], it[j], it[i]))       # fundo
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return objeto_de_bmesh(nome, bm, mat)


def placa(nome, larg, alt, prof, centro, mat, raio=0.002):
    """Placa fina de cantos arredondados, paralela à frente."""
    x, y, z = centro
    aneis = [[(px + x, py, pz) for px, py, pz in anel_xz(larg, alt, raio, y + dy, z, 1)]
             for dy in (-prof / 2, prof / 2)]
    return loft(nome, aneis, mat)


def no_eixo_y(ob, local):
    """Deita um sólido de revolução (eixo Z) para ficar com o eixo em -Y, na posição dada."""
    ob.rotation_euler = (math.radians(90), 0, 0)
    ob.location = local
    return ob


limpar_cena()

MADEIRA = material("MadeiraEscura", (0.20, 0.12, 0.07))
COBRE = material("Cobre", (0.80, 0.52, 0.26))
RIPA = material("Ripa", (0.50, 0.30, 0.14))
PAINEL = material("Painel", (0.10, 0.08, 0.07))
ESCALA = material("Escala", (0.90, 0.80, 0.56))
VERMELHO = material("Vermelho", (0.70, 0.16, 0.10))
PRETO = material("Preto", (0.09, 0.09, 0.10))
CROMADO = material("Cromado", (0.72, 0.73, 0.76))
LANTERNA = material("Lanterna", (0.88, 0.89, 0.90))

# --- Gabinete: caixa de cantos arredondados, com chanfro nas bordas ----------
cz = ALT / 2
gabinete = loft("Gabinete", [
    anel_xz(LARG - 0.010, ALT - 0.010, 0.022, Y_FRENTE, cz),
    anel_xz(LARG, ALT, 0.026, Y_FRENTE + 0.006, cz),
    anel_xz(LARG, ALT, 0.026, Y_FUNDO - 0.006, cz),
    anel_xz(LARG - 0.010, ALT - 0.010, 0.022, Y_FUNDO, cz),
], MADEIRA)
# Lanterna na lateral esquerda (painel claro com aro de cobre).
lateral = []
for nome, larg, alt, dx, mat in (("AroLanterna", 0.074, 0.122, 0.0, COBRE), ("Lanterna", 0.066, 0.114, -0.0015, LANTERNA)):
    p = placa(nome, larg, alt, 0.003, (0, 0, 0), mat, raio=0.008)
    p.rotation_euler = (0, 0, math.radians(90))
    p.location = (-LARG / 2 - 0.001 + dx, 0.004, cz)
    lateral.append(p)
for i in range(6):
    linha = placa(f"Friso{i}", 0.058, 0.0015, 0.001, (0, 0, 0), PAINEL)
    linha.rotation_euler = (0, 0, math.radians(90))
    linha.location = (-LARG / 2 - 0.003, 0.004, cz - 0.045 + i * 0.018)
    lateral.append(linha)
fixar([gabinete] + lateral)
gabinete = juntar("Gabinete", [gabinete] + lateral)
facetar(gabinete)

# --- Frente: moldura de cobre, painel recuado, grade de ripas e escala --------
INT_L, INT_A, INT_Z0 = 0.216, 0.135, 0.020      # vão interno da moldura
frente = [moldura("Moldura", (0.250, 0.165, 0.024), (INT_L, INT_A, 0.012), Y_FRENTE - 0.012, Y_FRENTE + 0.004,
                  cz, COBRE)]
frente.append(placa("Fundo", INT_L + 0.002, INT_A + 0.002, 0.002, (0, Y_FRENTE - 0.0005, cz), PAINEL))

# Ripas da grade do alto-falante: réguas retas, alternando a profundidade
# (a ondulação da foto vira um ritmo de facetas).
N_RIPAS = 22
for i in range(N_RIPAS):
    x = -0.0966 + i * (2 * 0.0966 / (N_RIPAS - 1))
    recuo = 0.0008 if i % 2 else -0.0008
    frente.append(placa(f"Ripa{i}", 0.0058, 0.063, 0.005, (x, Y_FRENTE - 0.0045 + recuo, 0.1185), RIPA, raio=0.0012))
frente.append(placa("Friso da grade", 0.208, 0.004, 0.003, (0, Y_FRENTE - 0.004, 0.083), COBRE))

# Painel do dial: três escalas (FM, AM, SW) como réguas com marcas, sem texto,
# e as etiquetas vermelhas das bandas nos dois lados.
frente.append(placa("Painel do dial", 0.208, 0.056, 0.003, (0, Y_FRENTE - 0.003, 0.052), PAINEL))
y_txt = Y_FRENTE - 0.0047
for banda, z, marcas in (("FM", 0.071, 7), ("AM", 0.061, 6), ("SW", 0.045, 6)):
    frente.append(placa(f"Escala {banda}", 0.090, 0.0012, 0.001, (0.0, y_txt, z - 0.003), ESCALA, raio=0.0004))
    for k in range(marcas):
        x = -0.040 + k * 0.080 / (marcas - 1)
        frente.append(placa(f"Marca {banda}{k}", 0.0045, 0.0035, 0.001, (x, y_txt, z + 0.0005), ESCALA, raio=0.0005))
    for lado in (-1, 1):
        frente.append(placa(f"Etiqueta {banda}{lado}", 0.011, 0.0062, 0.001, (lado * 0.054, y_txt + 0.0003, z), VERMELHO))
frente.append(placa("Linha das escalas", 0.112, 0.0008, 0.001, (0.002, y_txt, 0.0535), ESCALA, raio=0.0003))
fixar(frente)
frente = juntar("Frente", frente)
facetar(frente)

# --- Controles: dois botões giratórios, quatro teclas e uma chave -------------
controles = []
perfil_saia = [(0.0, 0.012), (0.017, 0.012), (0.0185, 0.010), (0.0185, 0.0), (0.0, 0.0)]
perfil_tampa = [(0.0, 0.0175), (0.0105, 0.0172), (0.0138, 0.0155), (0.0138, 0.012), (0.0, 0.012)]
for lado in (-1, 1):
    pos = (lado * 0.083, Y_FRENTE - 0.002, 0.046)
    # Saia de 12 lados: as facetas fazem o papel das estrias da foto.
    saia = no_eixo_y(torno(f"Saia{lado}", perfil_saia, PRETO, seg=12), pos)
    tampa = no_eixo_y(torno(f"Tampa{lado}", perfil_tampa, COBRE, seg=10), pos)
    controles += [saia, tampa]

for i in range(4):
    tecla = placa(f"Tecla{i}", 0.013, 0.0075, 0.006, (-0.039 + i * 0.019, Y_FRENTE - 0.005, 0.031), COBRE, raio=0.0035)
    controles.append(tecla)
controles.append(placa("Trilho da chave", 0.020, 0.006, 0.002, (0.045, Y_FRENTE - 0.0045, 0.031), PRETO))
controles.append(placa("Chave", 0.006, 0.008, 0.005, (0.041, Y_FRENTE - 0.006, 0.031), CROMADO, raio=0.0015))
fixar(controles)
controles = juntar("Controles", controles)
facetar(controles)

# --- Antena telescópica: nasce no topo, atrás à direita, e cruza para a esquerda
base = Vector((0.095, 0.035, ALT))
direcao = Vector((-1.0, 0.05, 0.24)).normalized()
antena = [cilindro("Pivo", 0.0065, 0.010, CROMADO, seg=8, centro=(base.x, base.y, base.z + 0.003))]
trechos = [(0.050, 0.0042, CROMADO), (0.045, 0.0036, CROMADO), (0.140, 0.0031, PRETO), (0.040, 0.0022, CROMADO)]
ponto = base + Vector((0, 0, 0.006))
for k, (comp, raio, mat) in enumerate(trechos):
    fim = ponto + direcao * comp
    antena.append(tubo(f"Trecho{k}", [tuple(ponto), tuple(fim)], raio, mat, lados=4))
    ponto = fim
ponta = cilindro("Ponta", 0.0038, 0.007, CROMADO, seg=6)
ponta.location = ponto
antena.append(ponta)
fixar(antena)
antena = juntar("Antena", antena)
facetar(antena)

# ----------------------------------------------------------------------------
# Exportação
# ----------------------------------------------------------------------------
paletizar("radio-retro-paleta", SAIDA.with_name("radio-retro-paleta.png"))
exportar("rádio", SAIDA)
if PREVIA:
    previa("rádio", PREVIA, camera=(-0.22, -0.58, 0.30), alvo=(-0.02, 0, 0.10))

# Antena de rastreio de sondas Bendix AN/GMD-1A para a Cena 2 · 1994 (Campanha Guará), low poly.
#
#   blender -b -P Ferramentas/blender/antena_gmd1.py -- [saida.fbx] [previa.png]
#
# Feita a partir das fotos da antena exposta no Centro de Lançamento da Barreira
# do Inferno (CLBI), onde rastreou radiossondas meteorológicas das campanhas de
# foguetes: tripé de pernas treliçadas com sapatas redondas, coluna central com
# o motor de azimute, caixa do mecanismo com as janelas do sincro, garfo de
# elevação, refletor parabólico de chapa perfurada com nervuras atrás e o
# alimentador no eixo, terminado na esfera do radome.
#
# Duas peças, como na antena real:
#   "Base"   tripé e coluna, fixos no chão;
#   "Cabeca" caixa, garfo, refletor e alimentador. A origem fica no rolamento de
#            azimute (topo da coluna, z = ROLAMENTO), sobre o eixo vertical: o
#            construtor de cenas gira só esta peça.
#   "Alimentador" (filho de "Cabeca") tubo e esfera do radome, com a origem no
#            vértice do refletor: o construtor tira dele o centro e o eixo do feixe.
#
# Medidas em metros, Z para cima, o refletor olhando para -Y (a frente).
import math
import sys
from pathlib import Path

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from comum import *  # noqa: E402,F403

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
SAIDA = Path(args[0]) if args else RAIZ / "Assets/TrilhaDaInovacao/Modelos/Guara/antena-gmd1.fbx"
PREVIA = Path(args[1]) if len(args) > 1 else None

limpar_cena()
BRANCO = material("Branco", (0.86, 0.87, 0.85))
CINZA = material("Cinza", (0.55, 0.57, 0.58))
ESCURO = material("Escuro", (0.08, 0.09, 0.1))
CONCRETO = material("Concreto", (0.62, 0.6, 0.56))

ROLAMENTO = 1.38           # topo da coluna, rolamento de azimute
EIXO_ELEVACAO = 2.28       # eixo horizontal do garfo
ELEVACAO = math.radians(55)  # o refletor aponta 55° acima do horizonte, como na foto
DIAMETRO = 2.4
FOCAL = 1.0                # profundidade do prato = R² / 4f ≈ 0,36 m
PE = 1.25                  # raio das sapatas do tripé


def caixa(nome, centro, tamanho, mat):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation(centro) @ Matrix.Diagonal((*tamanho, 1.0)))
    return objeto_de_bmesh(nome, bm, mat)


# --- Base: tripé treliçado e coluna ------------------------------------------
base = []
base.append(torno("Coluna", [(0.0, ROLAMENTO), (0.14, ROLAMENTO), (0.14, 1.26), (0.11, 1.22), (0.11, 0.98),
                              (0.19, 0.96), (0.19, 0.62), (0.15, 0.6), (0.15, 0.52), (0.2, 0.5),
                              (0.2, 0.44), (0.12, 0.42), (0.12, 0.2), (0.0, 0.2)], BRANCO, seg=12))
base.append(torno("Rolamento", [(0.0, ROLAMENTO + 0.03), (0.18, ROLAMENTO + 0.03), (0.18, ROLAMENTO - 0.03),
                                (0.0, ROLAMENTO - 0.03)], CINZA, seg=12))
for i in range(3):
    a = math.radians(90 + i * 120)
    d = Vector((math.cos(a), math.sin(a), 0.0))
    lado = Vector((-d.y, d.x, 0.0))
    pe = d * PE + Vector((0, 0, 0.12))
    alto = d * 0.17 + Vector((0, 0, 0.5))
    baixo = d * 0.13 + Vector((0, 0, 0.24))
    # Banzo de cima, banzo de baixo e a treliça em zigue-zague entre eles.
    base.append(tubo("Banzo superior", [alto, pe], 0.035, BRANCO))
    base.append(tubo("Banzo inferior", [baixo, pe - d * 0.05], 0.03, BRANCO))
    zz = []
    for k in range(5):
        t = k / 4
        cima = alto.lerp(pe, t)
        embaixo = baixo.lerp(pe - d * 0.05, t)
        zz.append(cima if k % 2 == 0 else embaixo)
    base.append(tubo("Treliça", zz, 0.014, BRANCO))
    base.append(tubo("Travessa", [alto + lado * 0.05, baixo + lado * 0.05], 0.014, BRANCO))
    sapata = torno("Sapata", [(0.0, 0.13), (0.04, 0.13), (0.04, 0.05), (0.13, 0.04), (0.14, 0.0), (0.0, 0.0)],
                   BRANCO, seg=10)
    sapata.location = (d.x * PE, d.y * PE, 0.0)
    base.append(sapata)
    bloco = cilindro("Bloco da sapata", 0.2, 0.06, CONCRETO, seg=8, centro=(d.x * PE, d.y * PE, -0.02))
    base.append(bloco)

fixar(base)
base = juntar("Base", base)
facetar(base)

# --- Cabeça: caixa do mecanismo, garfo e refletor -----------------------------
cabeca = []
# Caixa do mecanismo, com as duas janelas redondas do sincro na frente.
cabeca.append(caixa("Caixa", (0, 0, ROLAMENTO + 0.29), (0.56, 0.46, 0.52), BRANCO))
cabeca.append(caixa("Tampa", (0, 0, ROLAMENTO + 0.57), (0.6, 0.5, 0.04), BRANCO))
for x in (-0.12, 0.12):
    janela = cilindro("Janela", 0.045, 0.02, ESCURO, seg=10)
    janela.rotation_euler = (math.radians(90), 0, 0)
    janela.location = (x, -0.235, ROLAMENTO + 0.4)
    cabeca.append(janela)
cabeca.append(caixa("Placa", (0, -0.232, ROLAMENTO + 0.18), (0.3, 0.012, 0.12), CINZA))
# Garfo: suporte sobre a caixa e os dois braços até o eixo de elevação.
cabeca.append(caixa("Suporte do garfo", (0, 0, ROLAMENTO + 0.65), (0.5, 0.36, 0.12), BRANCO))
for x in (-0.3, 0.3):
    cabeca.append(tubo("Braço do garfo", [(x * 0.7, 0, ROLAMENTO + 0.7), (x, 0.0, EIXO_ELEVACAO)], 0.05, BRANCO,
                       lados=8))
    mancal = cilindro("Mancal", 0.08, 0.08, CINZA, seg=10)
    mancal.rotation_euler = (0, math.radians(90), 0)
    mancal.location = (x * 1.05, 0, EIXO_ELEVACAO)
    cabeca.append(mancal)

# Refletor. No referencial do prato o vértice fica na origem e a boca para +Z;
# depois ele é inclinado para a frente (-Y) e para cima (ELEVACAO).
direcao = Vector((0.0, -math.cos(ELEVACAO), math.sin(ELEVACAO)))
vertice = Vector((0.0, 0.0, EIXO_ELEVACAO)) + direcao * 0.22
giro_prato = Matrix.Translation(vertice) @ Matrix.Rotation(math.radians(90) - ELEVACAO, 4, "X")

R = DIAMETRO / 2
ANEIS, SEG, ESPESSURA = 6, 24, 0.035


def ponto(r, a, desloc=0.0):
    return Vector((r * math.cos(a), r * math.sin(a), r * r / (4 * FOCAL) - desloc))


bm = bmesh.new()
frente = [[bm.verts.new(ponto(R * k / ANEIS, 2 * math.pi * i / SEG)) for i in range(SEG)] for k in range(1, ANEIS + 1)]
tras = [[bm.verts.new(ponto(R * k / ANEIS, 2 * math.pi * i / SEG, ESPESSURA)) for i in range(SEG)]
        for k in range(1, ANEIS + 1)]
c_frente = bm.verts.new((0, 0, 0))
c_tras = bm.verts.new((0, 0, -ESPESSURA))
for i in range(SEG):
    j = (i + 1) % SEG
    bm.faces.new((c_frente, frente[0][i], frente[0][j]))
    bm.faces.new((c_tras, tras[0][j], tras[0][i]))
    for k in range(ANEIS - 1):
        bm.faces.new((frente[k][i], frente[k + 1][i], frente[k + 1][j], frente[k][j]))
        bm.faces.new((tras[k][j], tras[k + 1][j], tras[k + 1][i], tras[k][i]))
    bm.faces.new((frente[-1][i], tras[-1][i], tras[-1][j], frente[-1][j]))   # borda
bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
prato = objeto_de_bmesh("Refletor", bm, BRANCO)
prato.matrix_world = giro_prato
cabeca.append(prato)

# Aro de reforço na borda e nervuras radiais atrás da chapa perfurada.
aro = [ponto(R + 0.01, 2 * math.pi * i / SEG, ESPESSURA / 2) for i in range(SEG)]
aro_ob = tubo("Aro", aro, 0.03, BRANCO, lados=4, fechado=True)
aro_ob.matrix_world = giro_prato
cabeca.append(aro_ob)
for i in range(8):
    a = 2 * math.pi * (i + 0.5) / 8
    nervura = [ponto(R * k / 4, a, ESPESSURA + 0.04 * (1 - k / 4) + 0.02) for k in range(5)]
    ob = tubo("Nervura", nervura, 0.025, BRANCO, lados=4)
    ob.matrix_world = giro_prato
    cabeca.append(ob)
cubo = torno("Cubo do refletor", [(0.0, -ESPESSURA), (0.22, -ESPESSURA), (0.2, -0.2), (0.0, -0.22)], BRANCO, seg=10)
cubo.matrix_world = giro_prato
cabeca.append(cubo)
# Eixo de elevação ligando o cubo do prato aos mancais.
eixo = cilindro("Eixo de elevação", 0.05, 0.62, CINZA, seg=8)
eixo.rotation_euler = (0, math.radians(90), 0)
eixo.location = (0, 0, EIXO_ELEVACAO)
cabeca.append(eixo)

fixar(cabeca)
cabeca = juntar("Cabeca", cabeca)
facetar(cabeca)
# Origem no rolamento de azimute, sobre o eixo vertical.
cabeca.data.transform(Matrix.Translation((0, 0, -ROLAMENTO)))
cabeca.location = (0, 0, ROLAMENTO)

# Alimentador: tubo no eixo do refletor e a esfera do radome perto do foco.
alimentador = [
    tubo("Tubo do alimentador", [(0, 0, 0), (0, 0, 0.78)], 0.035, BRANCO, lados=8),
    torno("Radome", [(0.0, 0.98), (0.1, 0.95), (0.16, 0.86), (0.17, 0.78), (0.14, 0.7), (0.06, 0.65),
                     (0.0, 0.64)], BRANCO, seg=12),
]
fixar(alimentador)
alimentador = juntar("Alimentador", alimentador)
facetar(alimentador)
alimentador.matrix_world = giro_prato
alimentador.parent = cabeca
alimentador.matrix_parent_inverse = cabeca.matrix_world.inverted()

paletizar("antena-gmd1-paleta", SAIDA.with_name("antena-gmd1-paleta.png"))
exportar("antena-gmd1", SAIDA)
if PREVIA:
    previa("antena-gmd1", PREVIA, camera=(3.6, -5.2, 2.6), alvo=(0.0, 0.0, 1.6), lente=40)

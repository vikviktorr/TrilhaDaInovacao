# Robô guia (o companheiro da visita), low poly: drone flutuante.
# Referência: foto enviada pela equipe — cúpula azul achatada com o topo claro,
# visor escuro com dois olhos verticais ciano acesos, abas laterais erguidas como
# asas, corpo cinza com placas triangulares e luzes laranja sob a aba, um bocal
# central inclinado para a frente e dois propulsores laterais, todos com borda
# cromada e brilho azul na saída.
#
#   blender -b -P Ferramentas/blender/robo_guia.py -- [saida.fbx] [previa.png]
#
# Objetos exportados (o construtor de cenas e o GuideCharacter animam cada um):
#   Corpo    corpo cinza, propulsores e bocal (balança de leve com o resto)
#   Cabeca   cúpula, visor e abas, com a origem na base da cúpula (pulsa ao falar)
#   Luz      os dois olhos (o construtor troca por material emissivo ciano)
#   Jatos    saídas dos propulsores (emissivo azul)
#   Laranja  luzes laranja do corpo (emissivo laranja)
# Medidas: 0,56 m de largura na cúpula, 0,7 m com as abas e ~0,6 m de altura.
# Todo sólido sai fechado e com as normais para fora: o Unity descarta a face de
# trás, e qualquer casca aberta ou virada aparece como um buraco no modelo.
import math
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from comum import *  # noqa: E402,F403

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
SAIDA = Path(args[0]) if args else RAIZ / "Assets/TrilhaDaInovacao/Modelos/Robo/robo-guia.fbx"
PREVIA = Path(args[1]) if len(args) > 1 else None
if SAIDA.suffix.lower() != ".fbx":
    sys.exit(f"[robô guia] a saída tem de ser um .fbx: {SAIDA}")

# Perfil da cúpula (raio, z) de baixo para cima; achatada na frente e atrás.
CUPULA = [(0.272, 0.05), (0.28, 0.07), (0.276, 0.10), (0.262, 0.15), (0.232, 0.205), (0.185, 0.25),
          (0.12, 0.282), (0.05, 0.297), (0.0, 0.30)]
PROF = 0.9          # profundidade / largura
SEG = 24


def raio(z):
    for (r0, z0), (r1, z1) in zip(CUPULA, CUPULA[1:]):
        if z0 <= z <= z1:
            return r0 + (r1 - r0) * (z - z0) / (z1 - z0)
    return CUPULA[-1][0] if z > CUPULA[-1][1] else CUPULA[0][0]


def na_cupula(teta, z, afastar=0.0):
    """Ponto na superfície da cúpula; teta = 0 na frente (-Y), positivo para a esquerda (+X)."""
    r = raio(z)
    p = Vector((r * math.sin(teta), -r * math.cos(teta) * PROF, z))
    # Normal aproximada: horizontal para fora mais a inclinação do perfil.
    inclinacao = (raio(z - 0.005) - raio(z + 0.005)) / 0.01
    n = Vector((math.sin(teta), -math.cos(teta) / PROF, inclinacao)).normalized()
    return p + n * afastar, n


def casca(nome, mat, mat_topo):
    """Cúpula fechada por baixo; o topo acima de z = 0,248 sai na cor clara."""
    bm = bmesh.new()
    aneis = []
    for r, z in CUPULA:
        if r < 1e-6:
            aneis.append([bm.verts.new((0, 0, z))])
        else:
            aneis.append([bm.verts.new((r * math.sin(2 * math.pi * i / SEG), -r * math.cos(2 * math.pi * i / SEG) * PROF, z))
                          for i in range(SEG)])
    for a, b in zip(aneis, aneis[1:]):
        for i in range(SEG):
            j = (i + 1) % SEG
            if len(b) == 1:
                bm.faces.new((a[i], a[j], b[0]))
            else:
                bm.faces.new((a[i], a[j], b[j], b[i]))
    bm.faces.new(list(reversed(aneis[0])))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    ob = objeto_de_bmesh(nome, bm, mat)
    ob.data.materials.append(mat_topo)
    for p in ob.data.polygons:
        p.material_index = 1 if p.center.z > 0.248 else 0
    return ob


def remendo(nome, teta0, teta1, z0, z1, afastar, mat, nt=10, nz=4):
    """Placa curva com espessura que acompanha a cúpula (visor, moldura): um bloco
    fechado de nt × nz quadras, face de fora, de dentro e as quatro bordas."""
    bm = bmesh.new()
    fora, dentro = [], []
    for k in range(nz + 1):
        z = z0 + (z1 - z0) * k / nz
        lf, ld = [], []
        for i in range(nt + 1):
            p, n = na_cupula(teta0 + (teta1 - teta0) * i / nt, z, afastar)
            lf.append(bm.verts.new(p))
            ld.append(bm.verts.new(p - n * 0.006))
        fora.append(lf)
        dentro.append(ld)
    for k in range(nz):
        for i in range(nt):
            bm.faces.new((fora[k][i], fora[k][i + 1], fora[k + 1][i + 1], fora[k + 1][i]))
            bm.faces.new((dentro[k + 1][i], dentro[k + 1][i + 1], dentro[k][i + 1], dentro[k][i]))
    contorno = ([(0, i) for i in range(nt + 1)] + [(k, nt) for k in range(1, nz + 1)]
                + [(nz, i) for i in range(nt - 1, -1, -1)] + [(k, 0) for k in range(nz - 1, 0, -1)])
    for (k0, i0), (k1, i1) in zip(contorno, contorno[1:] + contorno[:1]):
        bm.faces.new((fora[k0][i0], dentro[k0][i0], dentro[k1][i1], fora[k1][i1]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return objeto_de_bmesh(nome, bm, mat)


def plaquinha(nome, teta, z, larg, alt, afastar, mat, raio_canto=0.012, espessura=0.006):
    """Retângulo arredondado colado na cúpula, virado para fora (os olhos)."""
    ob = loft(nome, [anel_retangular(larg, alt, raio_canto, -espessura / 2, 0, 2),
                     anel_retangular(larg, alt, raio_canto, espessura / 2, 0, 2)], mat)
    pos, n = na_cupula(teta, z, afastar)
    # Plano XY da placa -> perpendicular à normal, com o "alto" da placa para cima.
    eixo_z = n
    eixo_x = Vector((0, 0, 1)).cross(eixo_z).normalized()
    eixo_y = eixo_z.cross(eixo_x)
    ob.matrix_world = Matrix.Translation(pos) @ Matrix((
        (eixo_x.x, eixo_y.x, eixo_z.x, 0), (eixo_x.y, eixo_y.y, eixo_z.y, 0), (eixo_x.z, eixo_y.z, eixo_z.z, 0), (0, 0, 0, 1)))
    return ob


def caixa(nome, centro, tamanho, mat, raio=0.006, giro=(0.0, 0.0, 0.0)):
    sx, sy, sz = tamanho
    ob = loft(nome, [anel_retangular(sx, sy, raio, -sz / 2, 0, 1), anel_retangular(sx, sy, raio, sz / 2, 0, 1)], mat)
    ob.rotation_euler = giro
    ob.location = centro
    return ob


def prisma(nome, pontos, espessura, mat):
    """Polígono no plano XZ, extrudado em -Y."""
    return loft(nome, [[(x, 0, z) for x, z in pontos], [(x, -espessura, z) for x, z in pontos]], mat)


def bocal(nome, base, direcao, perfil, mat, seg=14):
    """Sólido de revolução (perfil (raio, distância) a partir da base) apontado na direção dada.
    O perfil tem de fechar: começar e terminar no eixo, ou repetir o 1º ponto no fim."""
    ob = torno(nome, [(r, -d) for r, d in perfil], mat, seg=seg)
    ob.matrix_world = Matrix.Translation(base) @ Vector((0, 0, -1)).rotation_difference(Vector(direcao).normalized()).to_matrix().to_4x4()
    return ob


limpar_cena()

AZUL = material("Azul", (0.06, 0.32, 0.78))
AZUL_CLARO = material("Topo", (0.62, 0.78, 0.9))
VISOR = material("Visor", (0.012, 0.02, 0.035), rugosidade=0.2)
MOLDURA = material("Moldura", (0.03, 0.17, 0.45))
CINZA = material("Cinza", (0.36, 0.38, 0.41))
GRAFITE = material("Grafite", (0.12, 0.13, 0.15))
CROMO = material("Cromo", (0.68, 0.7, 0.74), rugosidade=0.3)
OLHO = material("Olho", (0.4, 0.95, 1.0))
JATO = material("Jato", (0.3, 0.7, 1.0))
LARANJA = material("Laranja", (1.0, 0.42, 0.08))

corpo, cabeca, luz, jatos, laranja = [], [], [], [], []

# --- Cúpula, visor e abas ------------------------------------------------------
cabeca.append(casca("Cúpula", AZUL, AZUL_CLARO))
cabeca.append(remendo("Moldura do visor", -0.98, 0.98, 0.078, 0.215, 0.002, MOLDURA, nt=14, nz=4))
cabeca.append(remendo("Visor", -0.9, 0.9, 0.088, 0.205, 0.006, VISOR, nt=14, nz=4))
for lado in (-1, 1):
    luz.append(plaquinha("Olho", lado * 0.2, 0.148, 0.048, 0.094, 0.011, OLHO, raio_canto=0.018))
# Detalhes no topo: dois furinhos e uma seta em relevo, como na foto.
for dx in (-0.06, 0.06):
    p, _ = na_cupula(dx * 2, 0.268, 0.0)
    cabeca.append(cilindro("Furo", 0.01, 0.01, GRAFITE, seg=6, centro=(p.x, p.y + 0.01, p.z + 0.004)))
seta = loft("Seta", [[(-0.018, 0, 0), (0.018, 0, 0), (0, 0.022, 0)], [(-0.018, 0, 0.004), (0.018, 0, 0.004), (0, 0.022, 0.004)]], GRAFITE)
seta.location = (0.02, -0.06, 0.292)
seta.rotation_euler = (math.radians(-8), 0, 0)
cabeca.append(seta)
# Abas laterais: placas azuis presas no alto da cúpula, abertas para cima e para fora.
for lado in (-1, 1):
    aba = loft("Aba", [anel_retangular(0.17, 0.15, 0.04, -0.006, 0, 2), anel_retangular(0.16, 0.14, 0.035, 0.006, 0, 2)], AZUL)
    aba.rotation_euler = (0, lado * math.radians(-28), lado * math.radians(4))
    aba.location = (lado * 0.27, 0.01, 0.235)
    cabeca.append(aba)
    cabeca.append(caixa("Dobradiça", (lado * 0.2, 0.01, 0.255), (0.05, 0.08, 0.02), GRAFITE, raio=0.006,
                        giro=(0, lado * math.radians(-28), 0)))

# --- Corpo cinza sob a cúpula -------------------------------------------------
corpo.append(loft("Corpo", [
    anel_retangular(0.2, 0.18, 0.03, -0.11, 0, 2),
    anel_retangular(0.28, 0.24, 0.04, -0.05, 0, 2),
    anel_retangular(0.3, 0.26, 0.04, 0.05, 0, 2),
], CINZA))
# Placas gravadas: triângulos (um para cima, outro para baixo) e faixas escuras.
for dx, ponta in ((-0.07, 1), (0.07, -1)):
    tri = prisma("Placa triangular", [(dx - 0.03, 0.002 - ponta * 0.02), (dx + 0.03, 0.002 - ponta * 0.02),
                                      (dx, 0.002 + ponta * 0.02)], 0.006, GRAFITE)
    tri.location = (0, -0.126, 0)
    corpo.append(tri)
corpo.append(caixa("Faixa", (0, -0.124, 0.025), (0.22, 0.006, 0.012), GRAFITE, raio=0.002))
for lado in (-1, 1):
    corpo.append(caixa("Placa lateral", (lado * 0.146, 0, -0.01), (0.006, 0.16, 0.07), GRAFITE, raio=0.002))
    laranja.append(caixa("Luz laranja", (lado * 0.13, -0.11, 0.03), (0.035, 0.03, 0.025), LARANJA, raio=0.004))

# --- Propulsores -------------------------------------------------------------
# Bocal central: sai de baixo do corpo, dobra para a frente e aponta para baixo e para a frente.
corpo.append(bocal("Cotovelo", Vector((0, 0.0, -0.09)), (0, -0.04, -0.09),
                   [(0.0, 0.0), (0.045, 0.0), (0.045, 0.1), (0.0, 0.1)], GRAFITE, seg=10))
direcao_central = Vector((0, -0.55, -1)).normalized()
base_central = Vector((0, -0.04, -0.18))
corpo.append(bocal("Bocal", base_central, direcao_central,
                   [(0.0, 0.0), (0.05, 0.0), (0.062, 0.03), (0.07, 0.07), (0.0, 0.07)], GRAFITE))
corpo.append(bocal("Aro do bocal", base_central + direcao_central * 0.065, direcao_central,
                   [(0.075, 0.0), (0.077, 0.02), (0.06, 0.02), (0.058, 0.0), (0.075, 0.0)], CROMO))
jatos.append(bocal("Jato central", base_central + direcao_central * 0.07, direcao_central,
                   [(0.0, 0.0), (0.058, 0.0), (0.05, 0.02), (0.0, 0.03)], JATO))
# Propulsores laterais, mais baixos e um pouco para trás.
for lado in (-1, 1):
    base = Vector((lado * 0.155, 0.03, -0.07))
    direcao = Vector((lado * 0.15, 0.1, -1)).normalized()
    corpo.append(caixa("Suporte", (lado * 0.13, 0.03, -0.06), (0.06, 0.06, 0.04), GRAFITE, raio=0.008))
    corpo.append(bocal("Propulsor", base, direcao,
                       [(0.0, 0.0), (0.05, 0.0), (0.056, 0.03), (0.06, 0.09), (0.0, 0.09)], GRAFITE, seg=12))
    corpo.append(bocal("Aro", base + direcao * 0.085, direcao,
                       [(0.064, 0.0), (0.066, 0.016), (0.05, 0.016), (0.048, 0.0), (0.064, 0.0)], CROMO, seg=12))
    jatos.append(bocal("Jato", base + direcao * 0.09, direcao,
                       [(0.0, 0.0), (0.048, 0.0), (0.04, 0.015), (0.0, 0.022)], JATO, seg=12))

grupos = {"Corpo": corpo, "Cabeca": cabeca, "Luz": luz, "Jatos": jatos, "Laranja": laranja}
for lista in grupos.values():
    for ob in lista:
        facetar(ob)
    fixar(lista)
juntos = {nome: juntar(nome, lista) for nome, lista in grupos.items()}
# A cúpula pulsa a partir da base; os olhos vão junto.
for nome in ("Cabeca", "Luz"):
    ob = juntos[nome]
    bpy.context.scene.cursor.location = (0, 0, 0.05)
    for o in bpy.context.selected_objects:
        o.select_set(False)
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")

paletizar("robo-guia-paleta", SAIDA.with_name("robo-guia-paleta.png"))
exportar("robô guia", SAIDA)

if PREVIA:
    previa("robô guia", PREVIA, camera=(0.55, -1.25, 0.25), alvo=(0, 0, 0.05), lente=55)
    previa("robô guia", PREVIA.with_name(PREVIA.stem + "-perfil.png"), camera=(-1.1, -0.7, 0.55), alvo=(0, 0, 0.05), lente=55)

# Figurantes das cenas (pesquisador, empreendedora, estudante de muletas e
# engenheiro), low poly, no lugar dos mini-characters da Kenney.
#
# Referências pesquisadas: Quaternius (Ultimate Modular Men/Women), KayKit
# Adventurers e 100 Avatars — corpo de proporção adulta (~7 cabeças), facetado,
# cores lisas numa textura-paleta e 1,5–2,5 mil triângulos, o orçamento usual
# de humanos em VR/mobile. Cada parte do corpo segue um osso só (skin rígido,
# como nos modelos Kenney), o que dispensa pintar pesos e não deforma as juntas.
#
#   blender -b -P Ferramentas/blender/personagens.py -- [pasta de saída] [pasta das prévias]
#
# Rig (nomes compatíveis com Figurante e ApoioMuletas no Unity):
#   hips → torso → head · torso → arm-left → arm-left-lower (idem right)
#   hips → leg-left → leg-left-lower (idem right)
# A estudante tem ainda "muleta-ponta-left/right": a ponteira de cada muleta.
# Clipes: idle, interact-right, interact-left, emote-yes, emote-no.
# Medidas em metros, Z para cima, frente para -Y (o lado esquerdo do personagem é +X).
import math
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Quaternion, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from comum import *  # noqa: E402,F403

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
SAIDA = Path(args[0]) if args else RAIZ / "Assets/TrilhaDaInovacao/Modelos/Personagens"
PREVIAS = Path(args[1]) if len(args) > 1 else None

FPS = 24

# Ombro, quadril e alturas de um adulto de 1,70 m.
Z_OMBRO, Z_COTOVELO, Z_PUNHO = 1.42, 1.13, 0.88
Z_QUADRIL, Z_JOELHO, Z_TORNOZELO = 0.92, 0.50, 0.08
Z_PESCOCO = 1.47

PELES = {
    "clara": (0.80, 0.56, 0.42),
    "morena": (0.55, 0.33, 0.20),
    "parda": (0.66, 0.42, 0.28),
    "negra": (0.28, 0.15, 0.09),
}

PERSONAGENS = {
    # Opera a cortadora a laser no SENAI (2018): jaleco, óculos de proteção, barba.
    "pesquisador": dict(pele="morena", cabelo=(0.05, 0.035, 0.03), penteado="curto", barba=True,
                        camisa=(0.92, 0.93, 0.94), manga="longa", calca=(0.12, 0.14, 0.2), sapato=(0.08, 0.06, 0.05),
                        jaleco=True, oculos="protecao", ombro=0.40, quadril=0.31, peito=0.0),
    # Confere o notebook e comenta com o visitante: blazer, blusa clara, rabo de cavalo.
    "empreendedora": dict(pele="negra", cabelo=(0.03, 0.02, 0.02), penteado="rabo", barba=False,
                          camisa=(0.08, 0.42, 0.45), manga="longa", calca=(0.1, 0.1, 0.12), sapato=(0.35, 0.08, 0.06),
                          blusa=(0.95, 0.93, 0.88), oculos="grau", ombro=0.36, quadril=0.33, peito=0.025),
    # Estudante com muletas canadenses (2018): moletom, jeans, coque e mochila.
    "estudante": dict(pele="parda", cabelo=(0.25, 0.13, 0.06), penteado="coque", barba=False,
                      camisa=(0.42, 0.22, 0.58), manga="longa", calca=(0.18, 0.3, 0.55), sapato=(0.92, 0.92, 0.9),
                      mochila=(0.12, 0.35, 0.3), muletas=True, ombro=0.35, quadril=0.32, peito=0.02),
    # Engenheiro do CLA na reconstituição de 2025: polo azul-marinho, crachá e headset.
    "engenheiro": dict(pele="clara", cabelo=(0.35, 0.3, 0.27), penteado="curto", barba=False,
                       camisa=(0.08, 0.13, 0.3), manga="curta", calca=(0.55, 0.5, 0.38), sapato=(0.12, 0.09, 0.07),
                       cracha=True, headset=True, ombro=0.41, quadril=0.31, peito=0.0),
    # Técnico da TELMA no posto de operação e manutenção da Trópico R (1986): camisa
    # social de manga curta, calça de tergal, bigode e o crachá preso no bolso.
    "tecnico-telma": dict(pele="parda", cabelo=(0.04, 0.03, 0.025), penteado="curto", barba=False, bigode=True,
                          camisa=(0.68, 0.8, 0.9), manga="curta", calca=(0.3, 0.3, 0.32), sapato=(0.1, 0.07, 0.05),
                          cracha_bolso=True, ombro=0.40, quadril=0.31, peito=0.0),
    # Técnica no distribuidor geral, com o monofone de teste na mão: blusa clara,
    # calça azul-marinho e cabelo volumoso dos anos 1980.
    "tecnica-telma": dict(pele="morena", cabelo=(0.06, 0.035, 0.025), penteado="volume", barba=False,
                          camisa=(0.93, 0.88, 0.78), manga="curta", calca=(0.1, 0.13, 0.28), sapato=(0.2, 0.1, 0.06),
                          cracha_bolso=True, monofone=True, ombro=0.36, quadril=0.33, peito=0.025),
}


# --- Geometria -------------------------------------------------------------------

def secao(larg, prof, z, cx=0.0, cy=0.0, raio=None, seg=1):
    pts = anel_retangular(larg, prof, raio if raio is not None else min(larg, prof) * 0.4, z, cy, seg)
    return [(x + cx, y, zz) for x, y, zz in pts]


def membro(nome, cx, cy, perfil, mat, lados=6):
    """Tronco de cone facetado ao longo de Z: perfil = [(raio, z), ...] de cima para baixo."""
    ob = torno(nome, perfil, mat, seg=lados)
    ob.location = (cx, cy, 0)
    return ob


def caixa(nome, centro, tamanho, mat, raio=0.006, giro=(0.0, 0.0, 0.0)):
    sx, sy, sz = tamanho
    ob = loft(nome, [anel_retangular(sx, sy, raio, -sz / 2, 0, 1), anel_retangular(sx, sy, raio, sz / 2, 0, 1)], mat)
    ob.rotation_euler = giro
    ob.location = centro
    return ob


def recortar(ob, condicao):
    """Apaga as faces cujo centro satisfaz a condição (ex.: abrir o rosto no cabelo)."""
    bm = bmesh.new()
    bm.from_mesh(ob.data)
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if condicao(f.calc_center_median())], context="FACES")
    bm.to_mesh(ob.data)
    bm.free()


def montar(nome, p):
    """Cria as partes do personagem; devolve {osso: [objetos]}."""
    pele = material(f"{nome} pele", PELES[p["pele"]])
    cabelo = material(f"{nome} cabelo", p["cabelo"])
    camisa = material(f"{nome} camisa", p["camisa"])
    calca = material(f"{nome} calça", p["calca"])
    sapato = material(f"{nome} sapato", p["sapato"])
    olho = material("olho", (0.03, 0.03, 0.035))
    branco = material("branco", (0.93, 0.93, 0.92))
    cinza = material("cinza", (0.3, 0.31, 0.33))
    sola = material("sola", (0.9, 0.9, 0.88))

    partes = {k: [] for k in ("hips", "torso", "head", "arm-left", "arm-left-lower", "arm-right", "arm-right-lower",
                              "leg-left", "leg-left-lower", "leg-right", "leg-right-lower")}
    ombro, quadril, peito = p["ombro"], p["quadril"], p["peito"]

    # Tronco: da cintura ao pescoço, ombros largos e peito (busto discreto se peito > 0).
    partes["torso"].append(loft("Tronco", [
        secao(quadril, 0.21, Z_QUADRIL - 0.02),
        secao(quadril - 0.02, 0.2, 1.02),
        secao(ombro - 0.05, 0.21 + peito, 1.18, cy=-peito / 2),
        secao(ombro - 0.01, 0.22 + peito * 0.6, 1.30, cy=-peito / 3),
        secao(ombro, 0.21, Z_OMBRO - 0.02),
        secao(ombro - 0.1, 0.15, Z_PESCOCO - 0.02),
    ], camisa))
    partes["torso"].append(membro("Pescoço", 0, 0.005, [(0.045, Z_PESCOCO + 0.07), (0.05, Z_PESCOCO - 0.04)], pele))
    # Quadril e cós da calça.
    partes["hips"].append(loft("Quadril", [
        secao(quadril + 0.01, 0.21, Z_QUADRIL + 0.02),
        secao(quadril + 0.03, 0.22, 0.84),
        secao(quadril + 0.01, 0.2, 0.76),
    ], calca))

    if p.get("blusa"):
        # Blazer aberto: blusa clara em V na frente do peito.
        v = loft("Blusa", [secao(0.05, 0.02, 1.16), secao(0.13, 0.02, 1.40)], material(f"{nome} blusa", p["blusa"]))
        v.location = (0, -0.111 - peito * 0.55, 0)
        partes["torso"].append(v)
        for lado in (-1, 1):
            partes["torso"].append(caixa("Lapela", (lado * 0.065, -0.122 - peito * 0.5, 1.33), (0.035, 0.01, 0.14), camisa,
                                         giro=(0, lado * 0.0, lado * -0.35)))
    if p.get("jaleco"):
        # Saia do jaleco até a coxa, aberta na frente, e bolso com caneta.
        saia = loft("Jaleco", [secao(quadril + 0.035, 0.235, Z_QUADRIL + 0.04), secao(quadril + 0.07, 0.25, 0.66)], camisa,
                    tampa_baixo=False, tampa_cima=False)
        recortar(saia, lambda c: c.y < -0.1 and abs(c.x) < 0.04)
        # Com espessura: no Unity o lado de dentro de uma casca simples some (backface culling).
        engrossar(saia, 0.012)
        partes["hips"].append(saia)
        partes["torso"].append(caixa("Bolso", (0.08, -0.115, 1.27), (0.07, 0.006, 0.07), branco, raio=0.002))
        partes["torso"].append(caixa("Caneta", (0.095, -0.119, 1.31), (0.008, 0.008, 0.06), material("azul", (0.1, 0.2, 0.7)), raio=0.002))
        for lado in (-1, 1):
            partes["torso"].append(caixa("Gola", (lado * 0.05, -0.095, 1.42), (0.06, 0.03, 0.05), branco, giro=(0.3, 0, lado * 0.5)))
    if p.get("cracha"):
        partes["torso"].append(tubo("Cordão", [(-0.05, -0.08, 1.46), (-0.03, -0.12, 1.30), (0.0, -0.125, 1.22),
                                              (0.03, -0.12, 1.30), (0.05, -0.08, 1.46)], 0.004, material("cordão", (0.85, 0.3, 0.05)), lados=4))
        partes["torso"].append(caixa("Crachá", (0, -0.125, 1.18), (0.055, 0.006, 0.08), branco, raio=0.003))
        partes["torso"].append(caixa("Foto", (0, -0.129, 1.195), (0.025, 0.004, 0.03), cinza, raio=0.002))
    if p.get("cracha_bolso"):
        # Crachá de presilha no bolso da camisa (antes dos cordões).
        partes["torso"].append(caixa("Bolso", (0.08, -0.112 - peito * 0.4, 1.27), (0.08, 0.006, 0.08), camisa, raio=0.002))
        partes["torso"].append(caixa("Crachá", (0.08, -0.118 - peito * 0.4, 1.29), (0.05, 0.004, 0.07), branco, raio=0.003))
        partes["torso"].append(caixa("Faixa do crachá", (0.08, -0.121 - peito * 0.4, 1.312), (0.05, 0.002, 0.014),
                                     material("faixa do crachá", (0.1, 0.25, 0.6)), raio=0.001))
    if p.get("monofone"):
        # Monofone de teste (o "butt set" dos técnicos de linha) na mão direita, com os
        # jacarés pendurados no cabo.
        laranja = material("monofone", (0.95, 0.45, 0.08))
        x = -(ombro / 2 + 0.015)
        partes["arm-right-lower"].append(caixa("Monofone de teste", (x, -0.06, Z_PUNHO - 0.06), (0.05, 0.06, 0.2), laranja, raio=0.015))
        partes["arm-right-lower"].append(tubo("Cabo do monofone", [(x, -0.06, Z_PUNHO - 0.16), (x + 0.02, -0.03, Z_PUNHO - 0.3),
                                                                  (x + 0.04, -0.02, Z_PUNHO - 0.4)], 0.004, cinza, lados=4))
    if p.get("mochila"):
        mochila = material(f"{nome} mochila", p["mochila"])
        partes["torso"].append(caixa("Mochila", (0, 0.15, 1.17), (0.27, 0.11, 0.36), mochila, raio=0.03))
        partes["torso"].append(caixa("Bolso da mochila", (0, 0.21, 1.10), (0.2, 0.04, 0.14), mochila, raio=0.015))
        for lado in (-1, 1):
            partes["torso"].append(tubo("Alça", [(lado * 0.09, 0.1, 1.38), (lado * 0.1, -0.07, 1.43), (lado * 0.11, -0.12, 1.30),
                                                (lado * 0.12, -0.11, 1.08), (lado * 0.12, 0.1, 1.02)], 0.012, cinza, lados=4))

    # Cabeça: crânio, orelhas, nariz, olhos e sobrancelhas.
    cabeca = loft("Cabeça", [
        secao(0.10, 0.12, 1.50, cy=0.0),
        secao(0.15, 0.17, 1.56, cy=0.0),
        secao(0.165, 0.2, 1.63, cy=0.005),
        secao(0.16, 0.2, 1.69, cy=0.008),
        secao(0.11, 0.15, 1.725, cy=0.01),
    ], pele)
    partes["head"].append(cabeca)
    partes["head"].append(caixa("Nariz", (0, -0.103, 1.605), (0.025, 0.03, 0.04), pele, raio=0.006, giro=(-0.25, 0, 0)))
    for lado in (-1, 1):
        partes["head"].append(caixa("Olho", (lado * 0.037, -0.099, 1.632), (0.022, 0.006, 0.016), olho, raio=0.003))
        partes["head"].append(caixa("Sobrancelha", (lado * 0.037, -0.1, 1.655), (0.032, 0.008, 0.008), cabelo, raio=0.002))
        partes["head"].append(caixa("Orelha", (lado * 0.083, 0.005, 1.62), (0.018, 0.035, 0.05), pele, raio=0.006))
    if p.get("bigode"):
        partes["head"].append(caixa("Bigode", (0, -0.104, 1.578), (0.06, 0.012, 0.016), cabelo, raio=0.004))
    if p.get("barba"):
        barba = loft("Barba", [secao(0.155, 0.19, 1.60, cy=0.005), secao(0.13, 0.17, 1.535), secao(0.07, 0.1, 1.505, cy=-0.02)],
                     cabelo, tampa_cima=False)
        recortar(barba, lambda c: c.y > 0.02 or (abs(c.x) < 0.03 and c.z > 1.56))
        partes["head"].append(barba)

    # Cabelo: casca um pouco maior que o crânio, aberta no rosto.
    casca = loft("Cabelo", [
        secao(0.172, 0.21, 1.585, cy=0.012),
        secao(0.177, 0.215, 1.64, cy=0.008),
        secao(0.17, 0.212, 1.70, cy=0.01),
        secao(0.12, 0.165, 1.74, cy=0.012),
    ], cabelo, tampa_baixo=False)
    recortar(casca, lambda c: c.y < -0.06 and c.z < 1.675)
    partes["head"].append(casca)
    if p["penteado"] == "rabo":
        partes["head"].append(tubo("Rabo de cavalo", [(0, 0.1, 1.66), (0, 0.14, 1.6), (0, 0.13, 1.5), (0, 0.11, 1.44)], 0.03, cabelo, lados=6))
    elif p["penteado"] == "volume":
        # Cabelo cheio, armado em volta do rosto, até o pescoço.
        volume = loft("Volume", [secao(0.21, 0.22, 1.50, cy=0.03), secao(0.23, 0.25, 1.60, cy=0.02),
                                 secao(0.22, 0.25, 1.70, cy=0.015), secao(0.15, 0.19, 1.77, cy=0.015)], cabelo, tampa_baixo=False)
        recortar(volume, lambda c: c.y < -0.04 and c.z < 1.69)
        engrossar(volume, 0.01)
        partes["head"].append(volume)
    elif p["penteado"] == "coque":
        coque = torno("Coque", [(0.0, 0.08), (0.045, 0.06), (0.05, 0.03), (0.04, 0.0), (0.0, -0.01)], cabelo, seg=7)
        coque.location = (0, 0.06, 1.71)
        coque.rotation_euler = (-0.6, 0, 0)
        partes["head"].append(coque)

    if p.get("oculos") == "protecao":
        lente = material("lente", (0.55, 0.75, 0.8))
        partes["head"].append(caixa("Óculos de proteção", (0, -0.104, 1.632), (0.15, 0.02, 0.045), lente, raio=0.01))
        partes["head"].append(caixa("Elástico", (0, 0.0, 1.635), (0.17, 0.2, 0.012), cinza, raio=0.05))
    elif p.get("oculos") == "grau":
        aro = material("aro", (0.05, 0.05, 0.06))
        for lado in (-1, 1):
            partes["head"].append(caixa("Aro", (lado * 0.037, -0.104, 1.632), (0.04, 0.006, 0.03), aro, raio=0.006))
            partes["head"].append(caixa("Haste", (lado * 0.08, -0.05, 1.637), (0.004, 0.1, 0.005), aro, raio=0.001))
    if p.get("headset"):
        preto = material("headset", (0.04, 0.04, 0.045))
        partes["head"].append(tubo("Arco", [(-0.09, 0.0, 1.62), (-0.08, 0.0, 1.71), (0.0, 0.0, 1.755), (0.08, 0.0, 1.71), (0.09, 0.0, 1.62)],
                                   0.008, preto, lados=4))
        for lado in (-1, 1):
            partes["head"].append(disco_x("Fone", (lado * 0.092, 0.0, 1.615), 0.035, 0.025, preto))
        partes["head"].append(tubo("Microfone", [(-0.095, -0.01, 1.6), (-0.08, -0.07, 1.57), (-0.03, -0.1, 1.565)], 0.004, preto, lados=4))

    # Braços: manga, antebraço (pele ou manga), mão.
    manga_longa = p["manga"] == "longa"
    for lado, nome_lado in ((1, "left"), (-1, "right")):
        x = lado * (ombro / 2 + 0.015)
        partes[f"arm-{nome_lado}"].append(membro("Braço", x, 0.0, [(0.068, Z_OMBRO + 0.02), (0.064, Z_OMBRO - 0.08), (0.056, Z_COTOVELO)], camisa))
        if not manga_longa:
            partes[f"arm-{nome_lado}"].append(membro("Braço nu", x, 0.0, [(0.052, Z_OMBRO - 0.15), (0.05, Z_COTOVELO - 0.01)], pele))
        antebraco = camisa if manga_longa else pele
        partes[f"arm-{nome_lado}-lower"].append(membro("Antebraço", x, 0.0, [(0.054, Z_COTOVELO + 0.02), (0.044, Z_PUNHO + 0.01)], antebraco))
        if manga_longa and p.get("jaleco"):
            partes[f"arm-{nome_lado}-lower"].append(membro("Punho", x, 0.0, [(0.05, Z_PUNHO + 0.05), (0.05, Z_PUNHO + 0.01)], branco))
        partes[f"arm-{nome_lado}-lower"].append(caixa("Mão", (x, -0.005, Z_PUNHO - 0.05), (0.045, 0.085, 0.11), pele, raio=0.014))
        partes[f"arm-{nome_lado}-lower"].append(caixa("Polegar", (x - lado * 0.0, -0.045, Z_PUNHO - 0.035), (0.02, 0.02, 0.05), pele, raio=0.006))

    # Pernas e sapatos.
    for lado, nome_lado in ((1, "left"), (-1, "right")):
        x = lado * (quadril / 4 + 0.02)
        partes[f"leg-{nome_lado}"].append(membro("Coxa", x, 0.0, [(0.095, Z_QUADRIL - 0.06), (0.088, 0.72), (0.07, Z_JOELHO)], calca))
        partes[f"leg-{nome_lado}-lower"].append(membro("Canela", x, 0.0, [(0.07, Z_JOELHO + 0.02), (0.064, 0.36), (0.05, Z_TORNOZELO + 0.01)], calca))
        partes[f"leg-{nome_lado}-lower"].append(caixa("Sapato", (x, -0.035, 0.045), (0.11, 0.26, 0.085), sapato, raio=0.03))
        partes[f"leg-{nome_lado}-lower"].append(caixa("Sola", (x, -0.035, 0.008), (0.115, 0.265, 0.016), sola if p["sapato"][0] > 0.8 else cinza, raio=0.03))

    if p.get("muletas"):
        aluminio = material("alumínio", (0.62, 0.64, 0.68))
        azul = material("muleta azul", (0.1, 0.3, 0.75))
        borracha = material("borracha", (0.05, 0.05, 0.05))
        for lado, nome_lado in ((1, "left"), (-1, "right")):
            x = lado * (ombro / 2 + 0.015)
            xm = x + lado * 0.05
            dono = partes[f"arm-{nome_lado}-lower"]
            # Braçadeira atrás do antebraço, punho na altura da mão e haste até a ponteira.
            dono.append(caixa("Braçadeira", (x, 0.02, Z_COTOVELO - 0.05), (0.09, 0.03, 0.06), azul, raio=0.012))
            dono.append(caixa("Empunhadura", (x + lado * 0.025, -0.035, Z_PUNHO - 0.05), (0.08, 0.11, 0.03), azul, raio=0.01))
            dono.append(tubo("Haste de cima", [(x + lado * 0.03, 0.02, Z_COTOVELO - 0.05), (xm, 0.0, Z_PUNHO - 0.05)], 0.011, aluminio, lados=6))
            dono.append(tubo("Haste", [(xm, 0.0, Z_PUNHO - 0.05), (xm, 0.0, PONTA_MULETA + 0.04)], 0.012, aluminio, lados=6))
            dono.append(membro("Ponteira", xm, 0.0, [(0.017, PONTA_MULETA + 0.05), (0.02, PONTA_MULETA)], borracha))
    return partes


def engrossar(ob, espessura):
    mod = ob.modifiers.new("Espessura", "SOLIDIFY")
    mod.thickness = espessura
    mod.offset = 1.0
    for o in bpy.context.selected_objects:
        o.select_set(False)
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.modifier_apply(modifier=mod.name)


def disco_x(nome, centro, raio, espessura, mat):
    ob = cilindro(nome, raio, espessura, mat, seg=8)
    ob.rotation_euler = (0, math.pi / 2, 0)
    ob.location = centro
    return ob


# Ponteira da muleta com o braço na vertical: abaixo do chão, para a muleta
# alcançar o chão com o braço aberto ~24° (posição de tripé). O ApoioMuletas,
# no Unity, gira o braço até a ponteira tocar o chão.
PONTA_MULETA = Z_OMBRO - Z_OMBRO / math.cos(math.radians(24))


# --- Rig e animação ----------------------------------------------------------------

def criar_rig(nome, p):
    dados = bpy.data.armatures.new(f"{nome}-rig")
    rig = bpy.data.objects.new("Armature", dados)
    bpy.context.collection.objects.link(rig)
    for o in bpy.context.selected_objects:
        o.select_set(False)
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode="EDIT")
    ossos = {}

    def osso(n, cabeca, cauda, pai=None):
        b = dados.edit_bones.new(n)
        b.head, b.tail = Vector(cabeca), Vector(cauda)
        b.roll = 0.0
        if pai:
            b.parent = ossos[pai]
        ossos[n] = b

    osso("hips", (0, 0, Z_QUADRIL - 0.1), (0, 0, Z_QUADRIL))
    osso("torso", (0, 0, Z_QUADRIL), (0, 0, Z_PESCOCO), "hips")
    osso("head", (0, 0, Z_PESCOCO), (0, 0, 1.76), "torso")
    for lado, n in ((1, "left"), (-1, "right")):
        x = lado * (p["ombro"] / 2 + 0.015)
        osso(f"arm-{n}", (x, 0, Z_OMBRO), (x, 0, Z_COTOVELO), "torso")
        osso(f"arm-{n}-lower", (x, 0, Z_COTOVELO), (x, 0, Z_PUNHO - 0.1), f"arm-{n}")
        xl = lado * (p["quadril"] / 4 + 0.02)
        osso(f"leg-{n}", (xl, 0, Z_QUADRIL - 0.04), (xl, 0, Z_JOELHO), "hips")
        osso(f"leg-{n}-lower", (xl, 0, Z_JOELHO), (xl, 0, Z_TORNOZELO), f"leg-{n}")
        if p.get("muletas"):
            xm = x + lado * 0.05
            osso(f"muleta-ponta-{n}", (xm, 0, PONTA_MULETA), (xm, 0, PONTA_MULETA - 0.05), f"arm-{n}-lower")
    bpy.ops.object.mode_set(mode="OBJECT")
    return rig


def pose(rig, quadro, rotacoes):
    """rotacoes = {osso: [(eixo do mundo, graus), ...]}; grava a chave no quadro."""
    for nome, lista in rotacoes.items():
        pb = rig.pose.bones[nome]
        pb.rotation_mode = "QUATERNION"
        repouso = rig.data.bones[nome].matrix_local.to_3x3().inverted()
        q = Quaternion()
        for eixo, graus in lista:
            q = Quaternion((repouso @ Vector(eixo)).normalized(), math.radians(graus)) @ q
        pb.rotation_quaternion = q
        pb.keyframe_insert("rotation_quaternion", frame=quadro)


X, Y, Z = (1, 0, 0), (0, 1, 0), (0, 0, 1)
TODOS = ["hips", "torso", "head", "arm-left", "arm-left-lower", "arm-right", "arm-right-lower",
         "leg-left", "leg-left-lower", "leg-right", "leg-right-lower"]


def clipe(rig, nome, duracao, chaves):
    """chaves = [(quadro, {osso: [(eixo, graus)]})]; ossos ausentes ficam em repouso."""
    acao = bpy.data.actions.new(nome)
    acao.use_fake_user = True
    rig.animation_data_create()
    rig.animation_data.action = acao
    for quadro, rot in chaves:
        pose(rig, quadro, {o: rot.get(o, []) for o in TODOS})
    for fc in acao.fcurves:
        for k in fc.keyframe_points:
            k.interpolation = "BEZIER"
    acao.frame_range = (1, duracao)
    # Uma faixa NLA por clipe: no FBX a "take" sai só com o nome do clipe
    # (com bake_anim_use_all_actions viria "Armature|idle" e o Figurante não acharia).
    faixa = rig.animation_data.nla_tracks.new()
    faixa.name = nome
    faixa.strips.new(nome, 1, acao).name = nome
    return acao


def braco_baixo(extra=0.0):
    # Braços levemente afastados do corpo e cotovelo pouco dobrado (pose natural de pé).
    return {"arm-left": [(Y, -4 - extra)], "arm-right": [(Y, 4 + extra)],
            "arm-left-lower": [(X, -8)], "arm-right-lower": [(X, -8)]}


def animar(rig, muletas):
    def repouso(respiro=0.0, peso=0.0, olhar=0.0, extra=None):
        r = dict(braco_baixo(respiro * 1.5)) if not muletas else {}
        r["torso"] = [(X, 1.5 * respiro), (Y, peso * 1.2)]
        r["hips"] = [(Y, -peso)]
        r["head"] = [(Z, olhar), (X, -0.8 * respiro)]
        r["leg-left"] = [(Y, peso)]
        r["leg-right"] = [(Y, peso)]
        if extra:
            for k, v in extra.items():
                r[k] = r.get(k, []) + v
        return r

    # Respira e passa o peso de uma perna para a outra (80 quadros ≈ 3,3 s).
    clipe(rig, "idle", 81, [
        (1, repouso(0, 0, 0)), (21, repouso(1, 1.5, 4)), (41, repouso(0, 0, 0)),
        (61, repouso(1, -1.5, -4)), (81, repouso(0, 0, 0)),
    ])
    # Acena "sim" e "não" com a cabeça.
    clipe(rig, "emote-yes", 42, [(1, repouso()), (9, repouso(extra={"head": [(X, 14)]})), (16, repouso(extra={"head": [(X, -3)]})),
                                (23, repouso(extra={"head": [(X, 12)]})), (31, repouso(extra={"head": [(X, -2)]})), (42, repouso())])
    clipe(rig, "emote-no", 42, [(1, repouso()), (9, repouso(extra={"head": [(Z, 18)]})), (17, repouso(extra={"head": [(Z, -18)]})),
                               (25, repouso(extra={"head": [(Z, 15)]})), (33, repouso(extra={"head": [(Z, -10)]})), (42, repouso())])
    if muletas:
        return
    # Mexe na bancada à frente com uma das mãos (o tronco acompanha).
    for lado, nome in ((-1, "right"), (1, "left")):
        alcance = {f"arm-{nome}": [(X, -55), (Z, -lado * 12)], f"arm-{nome}-lower": [(X, -45)], "torso": [(X, 7), (Z, lado * 6)],
                   "head": [(X, 10)]}
        ajuste = {f"arm-{nome}": [(X, -48), (Z, -lado * 18)], f"arm-{nome}-lower": [(X, -55)], "torso": [(X, 8), (Z, lado * 8)],
                  "head": [(X, 12)]}
        base = braco_baixo()
        clipe(rig, f"interact-{nome}", 42, [
            (1, base), (12, {**base, **alcance}), (22, {**base, **ajuste}), (30, {**base, **alcance}), (42, base),
        ])


# --- Montagem e exportação ------------------------------------------------------------

def personagem(nome, p):
    limpar_cena()
    bpy.context.scene.render.fps = FPS
    partes = montar(nome, p)
    rig = criar_rig(nome, p)

    objetos = []
    for osso, lista in partes.items():
        for ob in lista:
            facetar(ob)
            objetos.append((osso, ob))
    # Cabeça 10% maior, a partir do pescoço: lê melhor de longe no óculos.
    pivo = Matrix.Translation((0, 0, Z_PESCOCO)) @ Matrix.Scale(1.1, 4) @ Matrix.Translation((0, 0, -Z_PESCOCO))
    for osso, ob in objetos:
        if osso == "head":
            ob.matrix_world = pivo @ ob.matrix_world
    fixar([ob for _, ob in objetos])
    for osso, ob in objetos:
        ob.vertex_groups.new(name=osso).add(range(len(ob.data.vertices)), 1.0, "REPLACE")
    corpo = juntar(nome, [ob for _, ob in objetos])
    corpo.parent = rig
    corpo.modifiers.new("Armature", "ARMATURE").object = rig

    paletizar(f"{nome}-paleta", SAIDA / f"{nome}-paleta.png")
    animar(rig, bool(p.get("muletas")))
    rig.animation_data.action = None

    tris = sum(len(f.vertices) - 2 for f in corpo.data.polygons)
    print(f"[personagens] {nome}: triângulos ≈ {tris}")
    for o in bpy.context.selected_objects:
        o.select_set(False)
    corpo.select_set(True)
    rig.select_set(True)
    destino = SAIDA / f"{nome}.fbx"
    destino.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=str(destino), use_selection=True, object_types={"ARMATURE", "MESH"},
        apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
        mesh_smooth_type="FACE", add_leaf_bones=False, primary_bone_axis="Y", secondary_bone_axis="X",
        bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=True,
        bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.5, path_mode="RELATIVE",
    )
    print(f"[personagens] FBX: {destino}")

    if PREVIAS:
        # Prévia num quadro do gesto principal, para conferir o rig.
        acao = "interact-right" if "interact-right" in bpy.data.actions else "emote-yes"
        rig.animation_data.action = bpy.data.actions[acao]
        bpy.context.scene.frame_set(12)
        previa(nome, PREVIAS / f"{nome}.png", camera=(1.2, -2.6, 1.4), alvo=(0, 0, 0.9), lente=45)


for nome, p in PERSONAGENS.items():
    personagem(nome, p)

# Peças do Centro de Lançamento de Alcântara (CLA) para a Cena 6 · 2025, low poly.
#
#   blender -b -P Ferramentas/blender/alcantara_cla.py -- <parte> [saida.fbx] [previa.png]
#
# Partes (cada uma vira um FBX em Modelos/Alcantara/):
# - foguete:    HANBIT-Nano da Innospace, dois estágios, 21,8 m de altura e 1,4 m
#               de diâmetro; 1º estágio híbrido (parafina + oxigênio líquido).
# - plataforma: base de concreto com fosso de chamas, mesa de lançamento, torre
#               umbilical em treliça com braços de serviço, tanques de oxigênio
#               líquido e metano, torres de iluminação e para-raios com cabo.
# - tmi:        Torre Móvel de Integração do CLA (33 x 10 x 13 m), sobre trilhos.
# - casamata:   prédio de controle avançado, baixo e de concreto, com antenas.
# Medidas em metros, Z para cima, frente (o lado do visitante) para -Y.
import math
import sys
from pathlib import Path

import bpy

sys.path.insert(0, str(Path(__file__).resolve().parent))
from comum import *  # noqa: E402,F403

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
PARTE = args[0] if args else "foguete"
NOMES = {"foguete": "foguete-hanbit-nano", "plataforma": "plataforma-hanbit", "tmi": "torre-movel-integracao",
         "casamata": "casamata"}
SAIDA = Path(args[1]) if len(args) > 1 else RAIZ / f"Assets/TrilhaDaInovacao/Modelos/Alcantara/{NOMES[PARTE]}.fbx"
PREVIA = Path(args[2]) if len(args) > 2 else None


def caixa(nome, centro, tamanho, mat, giro=(0, 0, 0)):
    sx, sy, sz = tamanho
    r = min(sx, sy) * 0.04
    ob = loft(nome, [anel_retangular(sx, sy, r, -sz / 2, 0, 1), anel_retangular(sx, sy, r, sz / 2, 0, 1)], mat)
    ob.rotation_euler = giro
    ob.location = centro
    return ob


def barra(nome, a, b, espessura, mat):
    """Perfil quadrado de a até b (treliças, braços, cabos rígidos)."""
    ax, ay, az = a
    bx, by, bz = b
    dx, dy, dz = bx - ax, by - ay, bz - az
    comp = math.sqrt(dx * dx + dy * dy + dz * dz)
    ob = caixa(nome, ((ax + bx) / 2, (ay + by) / 2, (az + bz) / 2), (espessura, espessura, comp), mat)
    # Alinha o eixo Z da caixa com a direção a→b.
    ob.rotation_euler = (0.0, math.acos(max(-1, min(1, dz / comp))), math.atan2(dy, dx))
    return ob


def cilindro_em(nome, centro, raio, altura, mat, seg=12, giro=(0, 0, 0)):
    ob = cilindro(nome, raio, altura, mat, seg=seg)
    ob.rotation_euler = giro
    ob.location = centro
    return ob


def finalizar(nome, pecas):
    fixar(pecas)
    ob = juntar(nome, pecas)
    facetar(ob)
    return ob


limpar_cena()
BRANCO = material("Branco", (0.92, 0.92, 0.93))
PRETO = material("Preto", (0.05, 0.05, 0.06))
CINZA = material("Cinza", (0.45, 0.46, 0.48))
GRAFITE = material("Grafite", (0.18, 0.18, 0.2))
CONCRETO = material("Concreto", (0.62, 0.61, 0.58))
CONCRETO_ESCURO = material("Concreto escuro", (0.36, 0.35, 0.34))
FULIGEM = material("Fuligem", (0.08, 0.07, 0.07))
LARANJA = material("Laranja", (0.85, 0.30, 0.08))
ACO = material("Aco", (0.70, 0.71, 0.72))
AZUL = material("Azul", (0.10, 0.22, 0.55))
VIDRO = material("Vidro", (0.05, 0.08, 0.12))
AMARELO = material("Amarelo", (0.90, 0.70, 0.08))

pecas = []

if PARTE == "foguete":
    # Origem na boca da tubeira; o Unity apoia esse ponto na mesa de lançamento.
    R = 0.7
    pecas += [
        torno("Tubeira", [(0.0, 1.0), (0.32, 1.0), (0.42, 0.55), (0.55, 0.0), (0.0, 0.0)], GRAFITE, seg=16),
        torno("Saia traseira", [(0.0, 2.0), (R, 2.0), (R + 0.03, 0.8), (0.0, 0.8)], CINZA, seg=16),
        cilindro_em("1º estágio", (0, 0, 7.3), R, 10.6, BRANCO, seg=16),
        cilindro_em("Faixa do 1º estágio", (0, 0, 3.2), R + 0.01, 0.35, PRETO, seg=16),
        cilindro_em("Interestágio", (0, 0, 12.9), R + 0.005, 0.6, PRETO, seg=16),
        cilindro_em("2º estágio", (0, 0, 15.3), R, 4.2, BRANCO, seg=16),
        cilindro_em("Base da coifa", (0, 0, 17.65), R + 0.01, 0.5, CINZA, seg=16),
        torno("Coifa", [(0.0, 21.8), (0.12, 21.7), (0.38, 21.0), (0.58, 20.1), (0.69, 19.1), (R + 0.01, 17.9),
                        (0.0, 17.9)], BRANCO, seg=16),
    ]
    # Canaleta de cabos ao longo do 1º estágio, do lado da torre (+X).
    pecas.append(caixa("Canaleta", (R + 0.04, 0, 7.4), (0.08, 0.16, 9.6), CINZA))
    # Aletas pequenas de estabilização na saia.
    for i in range(4):
        a = math.radians(45 + 90 * i)
        pecas.append(caixa("Aleta", (math.cos(a) * (R + 0.25), math.sin(a) * (R + 0.25), 1.6), (0.5, 0.06, 1.3), PRETO,
                           giro=(0, 0, a)))
    finalizar("Foguete", pecas)

elif PARTE == "plataforma":
    MESA = 2.0          # topo da mesa de lançamento (onde o foguete se apoia)
    TX = 4.6            # eixo da torre umbilical
    LADO = 2.4          # seção da torre
    ALT = 25.0          # altura da torre
    pecas += [
        caixa("Base de concreto", (0, 0, 0.3), (16, 16, 0.6), CONCRETO),
        caixa("Rampa", (0, -9.5, 0.15), (5, 3, 0.3), CONCRETO),
        # Fosso de chamas: rasgo no concreto que leva o jato para trás (+Y).
        caixa("Fosso de chamas", (0, 4.5, 0.58), (2.6, 9.0, 0.06), FULIGEM),
        caixa("Mancha de fuligem", (0, 0, 0.605), (4.2, 4.2, 0.02), FULIGEM),
    ]
    # Mesa de lançamento: quatro pés e o anel onde o foguete assenta.
    for sx in (-1, 1):
        for sy in (-1, 1):
            pecas.append(caixa("Pé da mesa", (sx * 1.3, sy * 1.3, 0.6 + (MESA - 0.6) / 2), (0.35, 0.35, MESA - 0.6), CINZA))
            pecas.append(barra("Garra", (sx * 1.3, sy * 1.3, MESA), (sx * 0.62, sy * 0.62, MESA + 0.7), 0.14, ACO))
    pecas.append(loft("Anel da mesa", [anel_retangular(3.2, 3.2, 0.2, MESA - 0.3, 0, 2), anel_retangular(3.2, 3.2, 0.2, MESA, 0, 2)],
                      CINZA))

    # Torre umbilical em treliça.
    cantos = [(TX + sx * LADO / 2, sy * LADO / 2) for sx, sy in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
    for x, y in cantos:
        pecas.append(barra("Coluna", (x, y, 0.6), (x, y, ALT), 0.22, LARANJA))
    andares = [0.6 + i * 2.5 for i in range(int((ALT - 0.6) / 2.5) + 1)]
    for k, z in enumerate(andares):
        for i in range(4):
            a, b = cantos[i], cantos[(i + 1) % 4]
            pecas.append(barra("Travessa", (a[0], a[1], z), (b[0], b[1], z), 0.12, LARANJA))
            if k + 1 < len(andares):
                z2 = andares[k + 1]
                if k % 2:
                    pecas.append(barra("Diagonal", (a[0], a[1], z), (b[0], b[1], z2), 0.08, LARANJA))
                else:
                    pecas.append(barra("Diagonal", (b[0], b[1], z), (a[0], a[1], z2), 0.08, LARANJA))
    # Plataformas de serviço (grade) e braços umbilicais que tocam o foguete.
    for z in (MESA + 3.0, MESA + 11.0, MESA + 16.0):
        pecas.append(caixa("Plataforma de serviço", (TX - 0.3, 0, z), (LADO + 0.9, LADO + 0.4, 0.12), GRAFITE))
        pecas.append(caixa("Guarda-corpo", (TX - 0.3, -(LADO + 0.4) / 2, z + 0.55), (LADO + 0.9, 0.05, 0.05), AMARELO))
    for z in (MESA + 11.0, MESA + 16.0):
        pecas.append(caixa("Braço umbilical", (TX - LADO / 2 - 1.2, 0, z), (2.3, 0.45, 0.35), CINZA))
        pecas.append(barra("Mangueira", (TX - LADO / 2 - 0.2, 0.15, z - 0.1), (0.85, 0.15, z - 0.6), 0.12, PRETO))
    pecas += [
        caixa("Topo da torre", (TX, 0, ALT + 0.15), (LADO + 0.3, LADO + 0.3, 0.3), LARANJA),
        barra("Para-raios da torre", (TX, 0, ALT + 0.3), (TX, 0, ALT + 3.0), 0.08, ACO),
        caixa("Elevador", (TX + LADO / 2 + 0.45, 0, 1.8), (0.8, 1.4, 2.4), CINZA),
    ]
    # Tanques de propelente: oxigênio líquido (branco) e metano (azul) para o 2º estágio.
    for nome, (x, y), mat in (("Tanque de LOX", (-8.5, 3.5), BRANCO), ("Tanque de metano", (-8.5, -1.0), AZUL)):
        pecas.append(cilindro_em(nome, (x, y, 2.2), 1.1, 5.5, mat, seg=12, giro=(0, math.radians(90), 0)))
        for dx in (-1.8, 1.8):
            pecas.append(caixa("Berço", (x + dx, y, 0.9), (0.4, 1.8, 0.6), CONCRETO_ESCURO))
        pecas.append(tubo("Linha de propelente", [(x + 2.75, y, 2.2), (-3.0, y * 0.5, 0.9), (TX - LADO / 2, 0.4, 0.9)], 0.12,
                          ACO, lados=6))
    # Torres de iluminação nos cantos (os refletores acesos são postos no Unity).
    for sx, sy in ((-1, -1), (1, -1), (-1, 1), (1, 1)):
        x, y = sx * 9.5, sy * 9.5
        pecas.append(barra("Poste de iluminação", (x, y, 0), (x, y, 15.0), 0.35, CINZA))
        pecas.append(caixa("Suporte dos refletores", (x, y, 15.2), (2.2, 0.3, 1.0), GRAFITE, giro=(0, 0, math.atan2(-y, -x) + math.pi / 2)))
    # Para-raios: dois mastros altos com um cabo entre eles, sobre a plataforma.
    for x in (-11.5, 11.5):
        pecas.append(barra("Mastro para-raios", (x, 6.5, 0), (x, 6.5, 32.0), 0.5, CINZA))
    pecas.append(tubo("Cabo para-raios", [(-11.5, 6.5, 31.5), (-5.0, 6.5, 29.3), (0, 6.5, 28.8), (5.0, 6.5, 29.3),
                                          (11.5, 6.5, 31.5)], 0.04, PRETO, lados=4))
    finalizar("Plataforma", pecas)

elif PARTE == "tmi":
    # Torre Móvel de Integração: estrutura alta e fechada que corre sobre trilhos
    # e abraça o foguete na montagem (33 m x 10 m x 13 m, 380 t).
    L, P, A = 10.0, 13.0, 33.0
    pecas += [
        caixa("Trilho esquerdo", (-3.5, 0, 0.1), (0.4, 50, 0.2), ACO),
        caixa("Trilho direito", (3.5, 0, 0.1), (0.4, 50, 0.2), ACO),
        caixa("Base de rodas", (0, 0, 0.8), (L + 0.6, P + 0.4, 1.2), GRAFITE),
        caixa("Corpo da torre", (0, 0, 1.4 + (A - 1.4) / 2), (L, P, A - 1.4), BRANCO),
        caixa("Vão da porta", (0, -P / 2 - 0.01, 14.0), (4.0, 0.1, 24.0), GRAFITE),
        caixa("Cobertura", (0, 0, A + 0.25), (L + 0.4, P + 0.4, 0.5), LARANJA),
        caixa("Ponte rolante", (0, 0, A + 1.2), (2.0, P - 2.0, 1.4), CINZA),
    ]
    # Faixas dos pavimentos e janelas em fita.
    for z in range(5, int(A), 4):
        pecas.append(caixa("Faixa de pavimento", (0, 0, z), (L + 0.12, P + 0.12, 0.3), LARANJA))
        for lado in (-1, 1):
            pecas.append(caixa("Janela", (lado * (L / 2 + 0.03), 0, z + 1.5), (0.06, P - 3.0, 0.7), VIDRO))
    finalizar("TMI", pecas)

elif PARTE == "casamata":
    # Prédio de controle avançado: concreto grosso, janelas estreitas e antenas.
    pecas += [
        caixa("Corpo", (0, 0, 1.8), (14, 8, 3.6), CONCRETO),
        caixa("Laje", (0, 0, 3.75), (14.6, 8.6, 0.3), CONCRETO_ESCURO),
        caixa("Porta blindada", (3.5, -4.01, 1.2), (1.6, 0.1, 2.4), CINZA),
        caixa("Marquise", (3.5, -4.6, 2.7), (2.6, 1.3, 0.15), CONCRETO_ESCURO),
        caixa("Placa", (-2.5, -4.02, 2.9), (4.0, 0.06, 0.6), AZUL),
    ]
    for x in (-5.5, -3.5, -1.5):
        pecas.append(caixa("Janela estreita", (x, -4.02, 2.2), (1.4, 0.06, 0.35), VIDRO))
    pecas += [
        barra("Mastro de antenas", (5.5, 2.5, 3.9), (5.5, 2.5, 11.0), 0.15, ACO),
        barra("Antena 1", (4.6, 2.5, 10.0), (6.4, 2.5, 10.0), 0.06, ACO),
        barra("Antena 2", (4.9, 2.5, 9.0), (6.1, 2.5, 9.0), 0.06, ACO),
        torno("Antena parabólica", [(0.0, 0.0), (0.9, 0.35), (0.95, 0.4), (0.0, 0.08)], BRANCO, seg=12),
        caixa("Ar-condicionado", (-4.5, 2.0, 4.2), (1.6, 1.2, 0.6), CINZA),
        caixa("Ar-condicionado", (-2.2, 2.0, 4.2), (1.6, 1.2, 0.6), CINZA),
    ]
    disco = pecas[-3]
    disco.rotation_euler = (math.radians(-55), 0, math.radians(20))
    disco.location = (1.5, 1.8, 4.6)
    finalizar("Casamata", pecas)

paletizar(f"{NOMES[PARTE]}-paleta", SAIDA.with_name(f"{NOMES[PARTE]}-paleta.png"))
exportar(PARTE, SAIDA)
if PREVIA:
    vistas = {"foguete": ((14, -22, 12), (0, 0, 10.5), 22), "plataforma": ((30, -42, 22), (0, 0, 9), 35),
              "tmi": ((45, -60, 25), (0, 0, 15), 35), "casamata": ((16, -22, 10), (0, 0, 2.5), 35)}
    cam, alvo, lente = vistas[PARTE]
    previa(PARTE, PREVIA, camera=cam, alvo=alvo, lente=lente)

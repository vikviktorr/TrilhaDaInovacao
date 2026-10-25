# Acabamento da sala da central Trópico R da TELMA (Cena 1 · 1986), low poly.
#
#   blender -b -P Ferramentas/blender/sala_telma.py -- [saida.fbx] [previa.png]
#
# A sala de equipamentos de uma central telefônica era um ambiente de trabalho: porta
# dupla para a entrada dos bastidores e a passagem para a sala de energia (retificadores
# e baterias de 48 V que alimentam a central), porta de serviço, janela com persiana,
# bancada de manutenção onde os técnicos testavam placas (multímetro, ferro de solda,
# lupa com lâmpada), armário de aço com placas sobressalentes, quadro de avisos com a
# escala de plantão, relógio de parede, extintor, arquivo de aço com o calendário e bebedouro.
# Tudo num modelo só (uma textura-paleta = um draw call), já nas coordenadas da sala do
# construtor de cenas: no Unity, com giro de 180° e escala 1, (x, y, z) = (x, z, y) do
# Blender. Paredes internas: x = -4,35 e 5,55; z (Unity) = -2,45 e 6,45; piso em 0.
# Os letreiros (SALA DE ENERGIA, SAÍDA...) são textos do Unity, sobre as placas daqui.
import math
import sys
from pathlib import Path

from mathutils import Matrix

sys.path.insert(0, str(Path(__file__).resolve().parent))
from comum import *  # noqa: E402,F403

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
SAIDA = Path(args[0]) if args else RAIZ / "Assets/TrilhaDaInovacao/Modelos/Telma/sala-telma.fbx"
PREVIA = Path(args[1]) if len(args) > 1 else None
if SAIDA.suffix.lower() != ".fbx":
    sys.exit(f"[sala telma] a saída tem de ser um .fbx: {SAIDA}")

X_ESQ, X_DIR, Z_FRENTE, Z_FUNDO = -4.35, 5.55, -2.45, 6.45


def caixa(nome, centro, tamanho, mat, raio=0.0):
    sx, sy, sz = tamanho
    ob = loft(nome, [anel_retangular(sx, sy, raio, -sz / 2, 0, 1), anel_retangular(sx, sy, raio, sz / 2, 0, 1)], mat)
    ob.location = centro
    return ob


def colocar(objetos, x, z_unity, giro, y=0.0):
    """Move um conjunto montado na origem (frente para -Y) para a sala: x e z do Unity,
    giro em graus (0 = de frente para o visitante, parede do fundo; -90 = parede direita)."""
    m = Matrix.Translation((x, z_unity, y)) @ Matrix.Rotation(math.radians(giro), 4, "Z")
    # location/rotation recém-definidas só chegam à matrix_world depois do update.
    bpy.context.view_layer.update()
    for ob in objetos:
        ob.matrix_world = m @ ob.matrix_world
    return objetos


limpar_cena()
BATENTE = material("Batente", (0.35, 0.33, 0.3))
PORTA = material("Porta", (0.55, 0.42, 0.28))          # madeira envernizada
PORTA_ACO = material("PortaAco", (0.62, 0.63, 0.6))
METAL = material("Metal", (0.72, 0.72, 0.7))
VIDRO = material("Vidro", (0.55, 0.65, 0.7))
PLACA_VERDE = material("PlacaVerde", (0.1, 0.5, 0.25))
PLACA_AZUL = material("PlacaAzul", (0.1, 0.18, 0.4))
PLACA_BRANCA = material("PlacaBranca", (0.92, 0.92, 0.88))
PERSIANA = material("Persiana", (0.85, 0.84, 0.78))
BANCADA = material("Bancada", (0.4, 0.36, 0.3))
ACO = material("Aco", (0.5, 0.52, 0.5))
CORTICA = material("Cortica", (0.6, 0.45, 0.28))
PAPEL = material("Papel", (0.95, 0.95, 0.9))
AMARELO = material("Amarelo", (0.95, 0.8, 0.2))
VERMELHO = material("Vermelho", (0.8, 0.1, 0.08))
PRETO = material("Preto", (0.06, 0.06, 0.07))
PLACA_PCB = material("PlacaPCB", (0.2, 0.36, 0.24))
LARANJA = material("Laranja", (0.95, 0.5, 0.1))
AZUL_AGUA = material("AzulAgua", (0.45, 0.65, 0.85))


def porta(largura, folhas, mat, visor=True):
    """Porta com batente, folha(s), maçaneta, chapa de chute e visor de vidro aramado."""
    obs = []
    alto = 2.1
    obs.append(caixa("Batente esquerdo", (-largura / 2 - 0.04, -0.02, alto / 2), (0.08, 0.06, alto), BATENTE))
    obs.append(caixa("Batente direito", (largura / 2 + 0.04, -0.02, alto / 2), (0.08, 0.06, alto), BATENTE))
    obs.append(caixa("Batente de cima", (0, -0.02, alto + 0.04), (largura + 0.16, 0.06, 0.08), BATENTE))
    lf = largura / folhas
    for k in range(folhas):
        cx = -largura / 2 + lf * (k + 0.5)
        obs.append(caixa("Folha", (cx, -0.02, alto / 2), (lf - 0.01, 0.04, alto - 0.01), mat))
        obs.append(caixa("Chapa de chute", (cx, -0.043, 0.15), (lf - 0.06, 0.006, 0.25), METAL))
        if visor:
            obs.append(caixa("Visor", (cx, -0.042, 1.55), (min(0.3, lf - 0.2), 0.006, 0.4), VIDRO))
        # Maçaneta do lado de dentro de cada folha (ao centro na dupla).
        lado = 1 if (folhas == 1 or k == 0) else -1
        mx = cx + lado * (lf / 2 - 0.08)
        obs.append(caixa("Espelho", (mx, -0.043, 1.0), (0.04, 0.006, 0.2), METAL))
        obs.append(caixa("Maçaneta", (mx - lado * 0.04, -0.07, 1.02), (0.11, 0.02, 0.022), METAL))
    return obs


def placa_letreiro(largura, altura, mat, z):
    return [caixa("Placa", (0, -0.015, z), (largura, 0.012, altura), mat)]


# --- Parede do fundo (Unity z = 6,45): porta dupla da sala de energia e extintor --------
obs = porta(1.6, 2, PORTA_ACO) + placa_letreiro(1.4, 0.24, PLACA_AZUL, 2.38)
colocar(obs, 2.1, Z_FUNDO, 0)
ext = [
    caixa("Suporte do extintor", (0, -0.01, 1.05), (0.12, 0.02, 0.2), METAL),
    cilindro("Extintor", 0.08, 0.5, VERMELHO, seg=10, centro=(0, -0.1, 0.75)),
    cilindro("Válvula", 0.025, 0.08, PRETO, seg=6, centro=(0, -0.1, 1.04)),
    caixa("Placa do extintor", (0, -0.01, 1.45), (0.25, 0.01, 0.3), VERMELHO),
    caixa("Faixa no piso", (0, -0.4, 0.002), (0.7, 0.7, 0.004), VERMELHO),
]
colocar(ext, 3.5, Z_FUNDO, 0)

# --- Parede direita (x = 5,55): porta de serviço, janela, bancada e armário --------------
obs = porta(0.9, 1, PORTA) + placa_letreiro(0.5, 0.18, PLACA_VERDE, 2.32)
colocar(obs, X_DIR, -1.3, -90)

janela = [
    caixa("Moldura da janela", (0, -0.03, 1.75), (1.8, 0.06, 1.3), BATENTE),
    caixa("Vidro", (0, -0.005, 1.75), (1.7, 0.01, 1.2), VIDRO),
    caixa("Peitoril", (0, -0.08, 1.08), (1.9, 0.12, 0.04), BATENTE),
    caixa("Caixa da persiana", (0, -0.07, 2.37), (1.8, 0.08, 0.08), PERSIANA),
]
# Persiana horizontal meio baixada: lâminas até 1,5 m, o resto mostra o vidro.
for k in range(14):
    janela.append(caixa("Lâmina", (0, -0.065, 2.3 - k * 0.058), (1.72, 0.05, 0.008), PERSIANA))
colocar(janela, X_DIR, 2.3, -90)

bancada = [
    caixa("Tampo da bancada", (0, -0.35, 0.86), (1.8, 0.7, 0.04), BANCADA),
    caixa("Gaveteiro", (0.6, -0.35, 0.42), (0.5, 0.66, 0.84), ACO),
    caixa("Pé", (-0.86, -0.35, 0.42), (0.05, 0.66, 0.84), ACO),
    caixa("Prateleira de baixo", (-0.15, -0.35, 0.12), (1.4, 0.6, 0.02), ACO),
    caixa("Painel de tomadas", (0, -0.03, 1.05), (1.8, 0.04, 0.3), ACO),
]
for k in range(3):
    bancada.append(caixa("Gaveta", (0.6, -0.685, 0.68 - k * 0.25), (0.46, 0.01, 0.22), ACO))
    bancada.append(caixa("Puxador", (0.6, -0.693, 0.74 - k * 0.25), (0.14, 0.012, 0.016), PRETO))
for k in range(6):
    bancada.append(caixa("Tomada", (-0.75 + k * 0.3, -0.055, 1.05), (0.07, 0.01, 0.1), PLACA_BRANCA))
# Sobre o tampo: placa em teste com o extensor, multímetro, ferro de solda no suporte,
# lupa com lâmpada articulada, caixa de componentes e o manual aberto.
bancada += [
    caixa("Placa em teste", (-0.3, -0.4, 0.89), (0.3, 0.22, 0.012), PLACA_PCB),
    caixa("Frente da placa", (-0.3, -0.52, 0.93), (0.3, 0.02, 0.07), PLACA_BRANCA),
    caixa("Multímetro", (0.1, -0.4, 0.9), (0.1, 0.18, 0.05), AMARELO, raio=0.01),
    caixa("Visor do multímetro", (0.1, -0.44, 0.927), (0.07, 0.05, 0.004), PRETO),
    caixa("Base do ferro", (0.35, -0.3, 0.9), (0.12, 0.1, 0.04), PRETO),
    caixa("Ferro de solda", (0.35, -0.36, 0.95), (0.025, 0.2, 0.025), LARANJA),
    caixa("Caixa de componentes", (0.65, -0.25, 0.92), (0.3, 0.2, 0.08), PLACA_AZUL),
    caixa("Manual aberto", (-0.68, -0.4, 0.885), (0.32, 0.24, 0.01), PAPEL),
]
bancada += [
    tubo("Braço da lupa", [(-0.78, -0.15, 0.88), (-0.75, -0.2, 1.3), (-0.55, -0.35, 1.25)], 0.012, METAL, lados=6),
]
lupa = torno("Lupa", [(0.0, 0.0), (0.09, 0.0), (0.1, -0.04), (0.0, -0.04)], PLACA_BRANCA, seg=12)
lupa.location = (-0.5, -0.4, 1.24)
bancada.append(lupa)
colocar(bancada, X_DIR, 2.3, -90)

armario = [caixa("Armário", (0, -0.23, 0.95), (0.9, 0.46, 1.9), ACO)]
for lado in (-1, 1):
    armario.append(caixa("Porta do armário", (lado * 0.225, -0.463, 0.97), (0.44, 0.01, 1.8), ACO))
    armario.append(caixa("Puxador", (lado * 0.03, -0.472, 1.0), (0.015, 0.012, 0.18), PRETO))
armario.append(caixa("Etiqueta", (-0.225, -0.47, 1.6), (0.2, 0.004, 0.06), PAPEL))
colocar(armario, X_DIR, 4.2, -90)

# --- Parede esquerda (x = -4,35): relógio sobre o posto e quadro de avisos -------------
relogio = torno("Relógio", [(0.0, 0.0), (0.16, 0.0), (0.17, -0.05), (0.0, -0.05)], PLACA_BRANCA, seg=16)
relogio.rotation_euler = (math.radians(-90), 0, 0)
relogio.location = (0, 0.0, 2.45)
ponteiros = [
    caixa("Aro", (0, -0.03, 2.45), (0.02, 0.01, 0.02), PRETO),
    caixa("Ponteiro das horas", (0.035, -0.055, 2.47), (0.08, 0.006, 0.012), PRETO),
    caixa("Ponteiro dos minutos", (0, -0.056, 2.53), (0.012, 0.006, 0.14), PRETO),
]
colocar([relogio] + ponteiros, X_ESQ, 1.6, 90)

quadro = [
    caixa("Quadro de avisos", (0, -0.015, 1.5), (1.0, 0.03, 0.7), CORTICA),
    caixa("Moldura", (0, -0.01, 1.5), (1.06, 0.02, 0.76), BATENTE),
    caixa("Escala de plantão", (-0.2, -0.033, 1.55), (0.3, 0.004, 0.42), PAPEL),
    caixa("Aviso", (0.22, -0.033, 1.62), (0.28, 0.004, 0.2), PAPEL),
    caixa("Aviso amarelo", (0.25, -0.033, 1.35), (0.22, 0.004, 0.16), AMARELO),
]
colocar(quadro, X_ESQ, -1.6, 90)

# --- Parede da frente (z = -2,45, atrás do visitante): porta de entrada ------------------
obs = porta(0.9, 1, PORTA) + placa_letreiro(0.5, 0.18, PLACA_VERDE, 2.32)
colocar(obs, -2.4, Z_FRENTE, 180)

# Arquivo de aço de 4 gavetas (fichas dos assinantes e prontuários das linhas), com o
# calendário de parede do ano por cima, e o bebedouro de garrafão ao lado.
arquivo = [caixa("Arquivo", (0, -0.31, 0.67), (0.47, 0.62, 1.34), ACO)]
for g in range(4):
    z = 0.18 + g * 0.32
    arquivo.append(caixa("Gaveta", (0, -0.625, z), (0.42, 0.012, 0.29), ACO))
    arquivo.append(caixa("Puxador", (0, -0.636, z + 0.06), (0.12, 0.014, 0.02), PRETO))
    arquivo.append(caixa("Etiqueta", (0, -0.633, z + 0.1), (0.08, 0.004, 0.035), PAPEL))
arquivo += [
    caixa("Calendário", (0, -0.006, 1.85), (0.36, 0.008, 0.5), PAPEL),
    caixa("Cabeçalho do calendário", (0, -0.012, 2.04), (0.36, 0.004, 0.1), VERMELHO),
]
for linha in range(5):
    arquivo.append(caixa("Semana", (0, -0.012, 1.68 + linha * 0.055), (0.3, 0.003, 0.012), PRETO))
colocar(arquivo, 0.6, Z_FRENTE, 180)

bebedouro = [
    caixa("Gabinete do bebedouro", (0, -0.17, 0.5), (0.32, 0.32, 1.0), PLACA_BRANCA),
    caixa("Bandeja", (0, -0.34, 0.82), (0.22, 0.06, 0.03), ACO),
    caixa("Torneira", (-0.05, -0.34, 0.9), (0.04, 0.04, 0.05), AZUL_AGUA),
    caixa("Torneira", (0.05, -0.34, 0.9), (0.04, 0.04, 0.05), VERMELHO),
]
# Garrafão de 20 L de boca para baixo, encaixado no gabinete.
garrafao = torno("Garrafão", [(0.0, 1.48), (0.12, 1.47), (0.14, 1.41), (0.14, 1.13), (0.06, 1.06),
                              (0.04, 1.0), (0.0, 1.0)], AZUL_AGUA, seg=12)
garrafao.location = (0, -0.17, 0)
bebedouro.append(garrafao)
colocar(bebedouro, 1.35, Z_FRENTE, 180)

obs = [o for o in bpy.data.objects if o.type == "MESH"]
for o in obs:
    facetar(o)
fixar(obs)
sala = juntar("Sala TELMA", obs)
paletizar("sala-telma-paleta", SAIDA.with_name("sala-telma-paleta.png"))
exportar("sala telma", SAIDA)
if PREVIA:
    previa("sala telma", PREVIA, camera=(0.6, -6.0, 9.0), alvo=(0.6, 2.0, 0.5), lente=20)

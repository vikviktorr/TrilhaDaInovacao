# Drone quadricóptero impresso em 3D (protótipo da Cena 4 · 2018 SENAI), low poly.
# Referência: foto da equipe — braços em treliça de PLA ciano com miolo preto,
# placa central preta com a controladora azul, motores com hélices pretas de
# duas pás e ogivas prateadas, alças de pouso ciano sob as pontas dos braços.
#
#   blender -b -P Ferramentas/blender/drone.py -- [saida.fbx] [previa.png]
#
# Objetos exportados: Corpo (estrutura inteira) e Helice1..4 (com a origem no
# eixo do motor, para girarem no Unity). Medidas em metros, Z para cima.
import math
import sys
from pathlib import Path

import bpy

sys.path.insert(0, str(Path(__file__).resolve().parent))
from comum import *  # noqa: E402,F403

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
SAIDA = Path(args[0]) if args else RAIZ / "Assets/TrilhaDaInovacao/Modelos/Drone/drone.fbx"
PREVIA = Path(args[1]) if len(args) > 1 else None

# Cor do PLA ciano; a peça na impressora usa a mesma (ConstrutorDeCenas, Cena 4).
CIANO_PLA = (0.04, 0.72, 0.85)


def caixa(nome, centro, tamanho, mat, giro_z=0.0, giro_y=0.0):
    sx, sy, sz = tamanho
    ob = loft(nome, [anel_retangular(sx, sy, 0.0005, -sz / 2, 0, 1), anel_retangular(sx, sy, 0.0005, sz / 2, 0, 1)], mat)
    ob.rotation_euler = (0.0, giro_y, giro_z)
    ob.location = centro
    return ob


limpar_cena()

CIANO = material("PLA ciano", CIANO_PLA)
PRETO = material("PLA preto", (0.03, 0.03, 0.035))
PLACA = material("Controladora", (0.05, 0.16, 0.45))
CHIP = material("Chip", (0.01, 0.01, 0.012))
PRATA = material("Prata", (0.72, 0.73, 0.76))
FIO_LARANJA = material("Fio", (0.85, 0.35, 0.05))

BRACO = 0.17          # do centro ao eixo do motor
Z_BRACO = 0.0
ALT_BRACO = 0.018

corpo = [
    # Placa central (preta) girada 45° em relação aos braços, como na foto.
    caixa("Placa central", (0, 0, 0.012), (0.11, 0.11, 0.004), PRETO, giro_z=math.radians(45)),
    caixa("Placa de baixo", (0, 0, -0.012), (0.09, 0.09, 0.004), PRETO, giro_z=math.radians(45)),
    caixa("Controladora", (0, 0, 0.0175), (0.06, 0.045, 0.003), PLACA, giro_z=math.radians(45)),
    caixa("Microcontrolador", (0.004, -0.004, 0.0215), (0.032, 0.01, 0.005), CHIP, giro_z=math.radians(45)),
    caixa("Servo", (-0.022, 0.022, 0.026), (0.03, 0.014, 0.022), CHIP, giro_z=math.radians(45)),
    caixa("Trava do servo", (-0.022, 0.022, 0.039), (0.04, 0.008, 0.004), CIANO, giro_z=math.radians(45)),
    tubo("Fios do servo", [(-0.02, 0.03, 0.04), (-0.005, 0.035, 0.045), (0.01, 0.02, 0.025)], 0.0015, FIO_LARANJA, lados=4),
]
# Antenas do receptor: dois tubos pretos que descem para a frente.
for lado in (-1, 1):
    corpo.append(tubo("Antena", [(lado * 0.012, -0.03, 0.012), (lado * 0.02, -0.06, -0.005), (lado * 0.03, -0.10, -0.03)],
                      0.002, PRETO, lados=4))
    corpo.append(caixa("Presilha da antena", (lado * 0.012, -0.034, 0.016), (0.008, 0.016, 0.006), CIANO))

for i in range(4):
    a = i * math.pi / 2
    ux, uy = math.cos(a), math.sin(a)
    inicio, fim = 0.04, BRACO - 0.012
    meio = (inicio + fim) / 2
    comp = fim - inicio
    centro = (ux * meio, uy * meio, Z_BRACO)
    # Treliça: banzos de cima e de baixo, miolo preto e diagonais em zigue-zague.
    for dz in (ALT_BRACO / 2, -ALT_BRACO / 2):
        for lateral in (-1, 1):
            off = lateral * 0.0075
            corpo.append(caixa("Banzo", (centro[0] - uy * off, centro[1] + ux * off, dz), (comp, 0.0045, 0.004), CIANO, giro_z=a))
    corpo.append(caixa("Miolo do braço", centro, (comp, 0.011, ALT_BRACO - 0.004), PRETO, giro_z=a))
    n = 6
    passo = comp / n
    for k in range(n):
        s = inicio + passo * (k + 0.5)
        inclinacao = math.atan2(ALT_BRACO, passo) * (1 if k % 2 == 0 else -1)
        for lateral in (-1, 1):
            off = lateral * 0.0075
            corpo.append(caixa("Diagonal", (ux * s - uy * off, uy * s + ux * off, 0.0),
                               (math.hypot(passo, ALT_BRACO), 0.004, 0.003), CIANO, giro_z=a, giro_y=0.0))
            corpo[-1].rotation_euler = (0.0, 0.0, 0.0)
            corpo[-1].rotation_mode = "ZYX"
            corpo[-1].rotation_euler = (0.0, -inclinacao, a)
    # Alça de pouso ciano sob a ponta do braço.
    p = BRACO - 0.045
    pts = [(ux * (p + 0.025 * math.cos(t)), uy * (p + 0.025 * math.cos(t)), -ALT_BRACO / 2 - 0.028 * math.sin(t))
           for t in [j * math.pi / 6 for j in range(7)]]
    corpo.append(tubo("Alça de pouso", pts, 0.002, CIANO, lados=4))
    # Suporte do motor (preto) e motor com a ogiva prateada.
    mx, my = ux * BRACO, uy * BRACO
    suporte = torno("Suporte do motor", [(0.0, 0.012), (0.014, 0.012), (0.016, -0.012), (0.0, -0.012)], PRETO, seg=10)
    suporte.location = (mx, my, 0.0)
    motor = cilindro("Motor", 0.013, 0.014, PRATA, seg=10)
    motor.location = (mx, my, 0.019)
    corpo += [suporte, motor]

fixar(corpo)
corpo = juntar("Corpo", corpo)
facetar(corpo)

# Hélices: duas pás pretas largas e torcidas, com a ogiva; origem no eixo.
for i in range(4):
    a = i * math.pi / 2
    pas = []
    for lado in (-1, 1):
        pa = caixa("Pá", (lado * 0.035, 0, 0), (0.06, 0.016, 0.0025), PRETO)
        pa.rotation_euler = (lado * math.radians(12), 0, 0)
        pas.append(pa)
        ponta = caixa("Ponta da pá", (lado * 0.072, 0, 0), (0.016, 0.011, 0.0022), PRETO)
        ponta.rotation_euler = (lado * math.radians(8), 0, 0)
        pas.append(ponta)
    cubo = cilindro("Cubo", 0.006, 0.006, PRETO, seg=8)
    ogiva = torno("Ogiva", [(0.0, 0.016), (0.0035, 0.009), (0.0055, 0.0), (0.0, 0.0)], PRATA, seg=8)
    ogiva.location.z = 0.003
    pas += [cubo, ogiva]
    fixar(pas)
    helice = juntar(f"Helice{i + 1}", pas)
    facetar(helice)
    helice.rotation_euler.z = a + math.radians(30 + 50 * i)
    helice.location = (math.cos(a) * BRACO, math.sin(a) * BRACO, 0.029)

paletizar("drone-paleta", SAIDA.with_name("drone-paleta.png"))
exportar("drone", SAIDA)
if PREVIA:
    previa("drone", PREVIA, camera=(0.42, -0.42, 0.36), alvo=(0.0, 0.0, 0.0), lente=40)

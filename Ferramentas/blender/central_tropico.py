# Equipamentos da central digital Trópico R da TELMA (Cena 1 · 1986), low poly.
#
#   blender -b -P Ferramentas/blender/central_tropico.py -- [pasta de saída] [pasta das prévias]
#
# O que se sabe da central (Telebrasil, nov./dez. 1986 e jan./fev. 1987):
# - Trópico R, central CPA-T (controle por programa armazenado, comutação temporal)
#   projetada pelo CPqD/Telebrás e fabricada pela PHT Sistemas Eletrônicos;
#   a da TELMA, em São Luís, tinha 1.008 terminais e entrou em operação comercial
#   em 13 de novembro de 1986, na estação Telma-1 (prefixo 232).
# - É modular: cada módulo (comutação, sinalização, terminais de assinante,
#   operação e manutenção) tem o próprio hardware e software, montado em placas que
#   encaixam pela frente em sub-bastidores ("gavetas") de bastidores de aço.
# - Foi projetada para dispensar ventilação forçada (clima brasileiro): os
#   bastidores não têm ventoinhas, só a convecção pelas grades de cima e de baixo.
# - A operação e a manutenção eram feitas num terminal de vídeo com teclado e uma
#   impressora de formulário contínuo para os relatórios; a PHT mostrou em 1986 a
#   supervisão remota da Telma-1 por um modem de 300 bit/s.
# - Os pares dos assinantes chegam ao distribuidor geral (DG): de um lado os blocos
#   dos cabos da rua, do outro os da central, ligados por fios de jumper que os
#   técnicos trocam à mão e testam com o monofone de teste.
# As medidas dos bastidores (2,2 × 0,6 × 0,45 m) seguem o padrão das centrais da
# Telebrás da época; não há foto publicada da Trópico R de São Luís.
#
# Saídas em Assets/TrilhaDaInovacao/Modelos/Telma/:
#   bastidor-tropico.fbx    um bastidor com 6 gavetas de placas e o painel de alarmes
#   posto-om.fbx            mesa de aço do posto de operação e manutenção, com terminal
#                           de vídeo, teclado, impressora matricial, modem e livro de ocorrências
#   distribuidor-geral.fbx  DG de 2 m com blocos de terminais e jumpers
# Medidas em metros, Z para cima, frente para -Y (no Unity, com giro de 180°, a frente
# fica para -Z, de frente para o visitante).
# Cotas que o construtor usa: no bastidor, a face das placas fica em y = -0,215 e as
# gavetas começam em z = 0,32, com 0,28 m de passo (GAVETA_Z0, GAVETA_PASSO).
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from comum import *  # noqa: E402,F403

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
PASTA = Path(args[0]) if args else RAIZ / "Assets/TrilhaDaInovacao/Modelos/Telma"
PREVIAS = Path(args[1]) if len(args) > 1 else None

ALTURA, LARGURA, PROF = 2.2, 0.6, 0.45
GAVETA_Z0, GAVETA_PASSO, GAVETAS = 0.32, 0.28, 6
Y_PLACAS = -0.215


def caixa(nome, centro, tamanho, mat, giro=(0.0, 0.0, 0.0), raio=None):
    sx, sy, sz = tamanho
    r = min(sx, sy) * 0.1 if raio is None else raio
    ob = loft(nome, [anel_retangular(sx, sy, r, -sz / 2, 0, 1), anel_retangular(sx, sy, r, sz / 2, 0, 1)], mat)
    ob.rotation_euler = giro
    ob.location = centro
    return ob


def cilindro_em(nome, centro, raio, altura, mat, seg=8, giro=(0.0, 0.0, 0.0)):
    ob = cilindro(nome, raio, altura, mat, seg=seg)
    ob.rotation_euler = giro
    ob.location = centro
    return ob


def fechar(rotulo, arquivo, camera, alvo, lente=40):
    obs = [o for o in bpy.data.objects if o.type == "MESH"]
    for o in obs:
        facetar(o)
    fixar(obs)
    juntar(rotulo, obs)
    saida = PASTA / f"{arquivo}.fbx"
    paletizar(f"{arquivo}-paleta", PASTA / f"{arquivo}-paleta.png")
    exportar(rotulo, saida)
    if PREVIAS:
        previa(rotulo, PREVIAS / f"{arquivo}.png", camera=camera, alvo=alvo, lente=lente)


# --- Bastidor da Trópico R ----------------------------------------------------------
def bastidor():
    limpar_cena()
    chapa = material("Chapa", (0.74, 0.73, 0.66))          # aço pintado bege-acinzentado
    escuro = material("Rodape", (0.16, 0.16, 0.17))
    gaveta = material("Gaveta", (0.55, 0.56, 0.54))
    placa = material("Placa", (0.2, 0.36, 0.24))            # fibra de vidro verde das placas
    frente_placa = material("FrentePlaca", (0.83, 0.82, 0.78))
    alavanca_azul = material("AlavancaAzul", (0.12, 0.25, 0.6))
    alavanca_branca = material("AlavancaBranca", (0.92, 0.92, 0.9))
    etiqueta = material("Etiqueta", (0.95, 0.94, 0.86))
    vermelho = material("LampadaVermelha", (0.85, 0.12, 0.08))
    amarelo = material("LampadaAmarela", (0.95, 0.75, 0.1))
    verde = material("LampadaVerde", (0.15, 0.7, 0.25))
    grade = material("Grade", (0.3, 0.3, 0.3))
    cabo = material("Cabo", (0.5, 0.5, 0.52))

    # Estrutura: montantes, laterais, teto e rodapé.
    caixa("Rodapé", (0, 0, 0.05), (LARGURA, PROF - 0.02, 0.1), escuro)
    for lado in (-1, 1):
        caixa("Lateral", (lado * (LARGURA / 2 - 0.01), 0, ALTURA / 2 + 0.05), (0.02, PROF, ALTURA - 0.1), chapa)
        caixa("Montante", (lado * (LARGURA / 2 - 0.035), Y_PLACAS + 0.0, ALTURA / 2 + 0.05), (0.03, 0.02, ALTURA - 0.1), chapa)
    caixa("Fundo", (0, PROF / 2 - 0.01, ALTURA / 2 + 0.05), (LARGURA - 0.04, 0.02, ALTURA - 0.1), chapa)
    caixa("Teto", (0, 0, ALTURA - 0.01), (LARGURA, PROF, 0.02), chapa)
    # Sem ventoinhas: grades de convecção embaixo e em cima.
    caixa("Grade de baixo", (0, Y_PLACAS + 0.004, 0.2), (LARGURA - 0.1, 0.01, 0.12), grade)
    caixa("Grade de cima", (0, Y_PLACAS + 0.004, 2.02), (LARGURA - 0.1, 0.01, 0.06), grade)

    # Painel de alarmes no topo: etiqueta do bastidor, três lâmpadas e a régua de fusíveis.
    caixa("Painel de alarmes", (0, Y_PLACAS + 0.01, 2.11), (LARGURA - 0.08, 0.03, 0.12), chapa)
    caixa("Etiqueta do bastidor", (-0.12, Y_PLACAS - 0.006, 2.12), (0.22, 0.004, 0.05), etiqueta)
    for k, mat in enumerate((vermelho, amarelo, verde)):
        cilindro_em("Lâmpada", (0.09 + k * 0.055, Y_PLACAS - 0.01, 2.12), 0.014, 0.02, mat, seg=8, giro=(math.radians(90), 0, 0))
    for k in range(8):
        caixa("Fusível", (-0.2 + k * 0.057, Y_PLACAS - 0.004, 2.075), (0.03, 0.01, 0.018), escuro)

    # Gavetas de placas: trilhos, placas verdes com frente clara, alavancas de extração
    # coloridas por tipo e a etiqueta de cada gaveta.
    for g in range(GAVETAS):
        z0 = GAVETA_Z0 + g * GAVETA_PASSO
        altura_placa = GAVETA_PASSO - 0.05
        caixa("Trilho de baixo", (0, Y_PLACAS + 0.02, z0 + 0.01), (LARGURA - 0.1, 0.04, 0.02), gaveta)
        caixa("Trilho de cima", (0, Y_PLACAS + 0.02, z0 + GAVETA_PASSO - 0.02), (LARGURA - 0.1, 0.04, 0.02), gaveta)
        caixa("Etiqueta da gaveta", (0, Y_PLACAS - 0.002, z0 + GAVETA_PASSO - 0.02), (0.3, 0.004, 0.014), etiqueta)
        n = 16
        passo = (LARGURA - 0.12) / n
        for c in range(n):
            x = -(LARGURA - 0.12) / 2 + passo * (c + 0.5)
            zc = z0 + 0.02 + altura_placa / 2
            # Caixas sem chanfro (12 triângulos cada): são ~300 peças por bastidor.
            caixa("Frente da placa", (x, Y_PLACAS, zc), (passo - 0.006, 0.008, altura_placa), frente_placa, raio=0)
            alavanca = alavanca_azul if (c + g) % 4 else alavanca_branca
            caixa("Alavanca", (x, Y_PLACAS - 0.01, z0 + 0.04), (passo - 0.012, 0.014, 0.025), alavanca, raio=0)
            caixa("Alavanca", (x, Y_PLACAS - 0.01, z0 + altura_placa), (passo - 0.012, 0.014, 0.025), alavanca, raio=0)
        # O verde das placas aparece pelas frestas entre as frentes: um fundo só por gaveta.
        caixa("Placas", (0, Y_PLACAS + 0.02, z0 + 0.02 + altura_placa / 2), (LARGURA - 0.12, 0.02, altura_placa), placa, raio=0)

    # Cabos que sobem para a esteira do teto.
    for k in range(5):
        tubo("Cabo", [(-0.16 + k * 0.08, 0.12, ALTURA - 0.02), (-0.16 + k * 0.08, 0.12, ALTURA + 0.22)], 0.012, cabo, lados=6)
    fechar("Bastidor Trópico R", "bastidor-tropico", camera=(1.4, -2.6, 1.6), alvo=(0, 0, 1.1))


# --- Posto de operação e manutenção --------------------------------------------------
def posto_om():
    limpar_cena()
    aco = material("Aco", (0.52, 0.54, 0.52))
    tampo = material("Tampo", (0.36, 0.33, 0.28))           # fórmica marrom
    bege = material("Bege", (0.82, 0.78, 0.66))
    bege_escuro = material("BegeEscuro", (0.6, 0.56, 0.47))
    tecla = material("Tecla", (0.3, 0.29, 0.27))
    tela = material("Tela", (0.03, 0.08, 0.04))
    fosforo = material("Fosforo", (0.3, 0.95, 0.4))          # texto verde do monitor de fósforo
    papel = material("Papel", (0.95, 0.95, 0.92))
    zebrado = material("Zebrado", (0.75, 0.88, 0.76))        # faixas verdes do formulário contínuo
    led = material("LedVermelho", (0.9, 0.12, 0.08))
    capa = material("CapaLivro", (0.1, 0.18, 0.4))
    preto = material("Preto", (0.06, 0.06, 0.07))

    # Mesa de aço: tampo de fórmica, gaveteiro à direita e painel frontal.
    caixa("Tampo", (0, 0, 0.735), (1.4, 0.72, 0.03), tampo)
    caixa("Gaveteiro", (0.48, 0, 0.36), (0.42, 0.68, 0.72), aco)
    for k in range(3):
        caixa("Gaveta", (0.48, -0.345, 0.6 - k * 0.22), (0.38, 0.01, 0.19), aco)
        caixa("Puxador", (0.48, -0.353, 0.66 - k * 0.22), (0.12, 0.012, 0.015), preto)
    caixa("Pé esquerdo", (-0.67, 0, 0.36), (0.04, 0.68, 0.72), aco)
    caixa("Painel de fundo", (-0.1, 0.33, 0.45), (1.1, 0.02, 0.5), aco)

    # Terminal de vídeo: gabinete bege, tubo afinando atrás, tela verde com linhas de texto.
    caixa("Gabinete do terminal", (-0.28, 0.0, 0.94), (0.42, 0.34, 0.38), bege, raio=0.02)
    loft("Traseira do tubo", [anel_retangular(0.36, 0.3, 0.03, 0.0, 0.0, 1), anel_retangular(0.22, 0.16, 0.03, 0.22, 0.0, 1)], bege_escuro)
    tras = bpy.data.objects["Traseira do tubo"]
    tras.rotation_euler = (math.radians(-90), 0, 0)
    tras.location = (-0.28, 0.17, 0.95)
    caixa("Moldura da tela", (-0.28, -0.172, 0.96), (0.36, 0.006, 0.27), bege_escuro, raio=0.02)
    caixa("Tela", (-0.28, -0.177, 0.965), (0.3, 0.006, 0.22), tela, raio=0.02)
    for k in range(9):
        largura = 0.24 - (k * 37 % 5) * 0.03
        caixa("Linha de texto", (-0.4 + largura / 2, -0.181, 1.055 - k * 0.022), (largura, 0.002, 0.008), fosforo, raio=0.0005)
    caixa("Cursor", (-0.385, -0.181, 0.855), (0.012, 0.002, 0.012), fosforo, raio=0.0005)
    caixa("Base do terminal", (-0.28, 0.0, 0.765), (0.3, 0.26, 0.03), bege_escuro)

    # Teclado separado, com as fileiras de teclas.
    caixa("Teclado", (-0.28, -0.25, 0.765), (0.46, 0.18, 0.03), bege, giro=(math.radians(4), 0, 0))
    for fila in range(5):
        caixa("Fileira de teclas", (-0.3, -0.31 + fila * 0.03, 0.784 + fila * 0.002), (0.36, 0.022, 0.012), tecla, raio=0.003)
    caixa("Teclado numérico", (-0.1, -0.25, 0.786), (0.07, 0.13, 0.012), tecla, raio=0.003)

    # Impressora matricial de 132 colunas com formulário contínuo zebrado.
    caixa("Impressora", (0.33, 0.06, 0.82), (0.5, 0.34, 0.14), bege, raio=0.015)
    caixa("Tampa da impressora", (0.33, 0.02, 0.9), (0.44, 0.2, 0.02), bege_escuro, giro=(math.radians(-8), 0, 0))
    caixa("Folha impressa", (0.33, 0.12, 0.99), (0.38, 0.004, 0.2), papel, giro=(math.radians(-15), 0, 0))
    for k in range(4):
        caixa("Faixa verde", (0.33, 0.115 - k * 0.0, 0.92 + k * 0.045), (0.38, 0.003, 0.02), zebrado, giro=(math.radians(-15), 0, 0))

    # Modem de 300 bit/s da supervisão remota, com a fileira de LEDs.
    caixa("Modem", (-0.58, -0.02, 0.775), (0.18, 0.24, 0.05), bege_escuro, raio=0.01)
    for k in range(5):
        caixa("LED do modem", (-0.64 + k * 0.03, -0.142, 0.785), (0.008, 0.004, 0.008), led, raio=0.001)
    tubo("Cabo do modem", [(-0.58, 0.1, 0.77), (-0.5, 0.25, 0.755), (-0.28, 0.2, 0.755)], 0.004, preto, lados=4)

    # Livro de ocorrências aberto e caneta.
    caixa("Livro aberto", (0.3, -0.22, 0.755), (0.36, 0.22, 0.01), papel)
    caixa("Capa do livro", (0.3, -0.22, 0.749), (0.38, 0.23, 0.006), capa)
    caixa("Caneta", (0.42, -0.2, 0.762), (0.12, 0.008, 0.008), preto, giro=(0, 0, math.radians(30)))
    fechar("Posto de O&M", "posto-om", camera=(1.6, -2.0, 1.6), alvo=(0, 0, 0.8))


# --- Distribuidor geral (DG) ----------------------------------------------------------
def distribuidor():
    limpar_cena()
    ferro = material("Ferro", (0.32, 0.34, 0.36))
    bloco = material("Bloco", (0.82, 0.8, 0.74))
    terminal = material("Terminal", (0.65, 0.6, 0.45))
    cores = [material(f"Jumper{k}", c) for k, c in enumerate(((0.85, 0.12, 0.1), (0.95, 0.95, 0.92),
                                                               (0.95, 0.78, 0.1), (0.1, 0.25, 0.7)))]
    cabo_rua = material("CaboRua", (0.08, 0.08, 0.08))
    largura, alto = 2.0, 2.2

    # Armação: montantes a cada 0,5 m, travessas e a régua de argolas dos jumpers.
    for k in range(5):
        x = -largura / 2 + k * 0.5
        caixa("Montante", (x, 0, alto / 2), (0.04, 0.04, alto), ferro)
    for z in (0.08, alto - 0.04):
        caixa("Travessa", (0, 0, z), (largura + 0.04, 0.05, 0.04), ferro)
    for z in (0.95, 1.85):
        caixa("Calha dos jumpers", (0, -0.06, z), (largura, 0.04, 0.03), ferro)

    # Blocos de terminais nas colunas (lado da rua em cima, lado da central embaixo).
    rng_cor = 0
    for k in range(4):
        x0 = -largura / 2 + 0.25 + k * 0.5
        for j in range(6):
            z = 1.05 + j * 0.13
            caixa("Bloco", (x0, -0.03, z), (0.38, 0.04, 0.1), bloco)
            caixa("Fileira de terminais", (x0, -0.052, z), (0.34, 0.006, 0.02), terminal)
        for j in range(5):
            z = 0.2 + j * 0.14
            caixa("Bloco da central", (x0, -0.03, z), (0.38, 0.04, 0.1), bloco)
            caixa("Fileira de terminais", (x0, -0.052, z), (0.34, 0.006, 0.02), terminal)
        # Cabos da rua descem por trás até o piso.
        tubo("Cabo da rua", [(x0, 0.06, alto), (x0, 0.06, 1.05)], 0.03, cabo_rua, lados=6)
        # Jumpers: do lado da rua até o lado da central, pendurados em curva.
        for j in range(5):
            xa = x0 - 0.15 + j * 0.07
            xb = x0 - 0.1 + ((j * 3 + k) % 5) * 0.06 + (0.5 if (j + k) % 3 == 0 and k < 3 else 0.0)
            za, zb = 1.05 + (j % 6) * 0.13, 0.2 + ((j + k) % 5) * 0.14
            tubo("Jumper", [(xa, -0.06, za), ((xa + xb) / 2, -0.11, (za + zb) / 2 - 0.05), (xb, -0.06, zb)], 0.0025,
                 cores[rng_cor % 4], lados=4)
            rng_cor += 1
    fechar("Distribuidor geral", "distribuidor-geral", camera=(1.8, -3.4, 1.5), alvo=(0, 0, 1.0))


bastidor()
posto_om()
distribuidor()

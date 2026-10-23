# Mapa da Ilha de São Luís (Upaon-Açu) em relevo low poly, deitado na mesa-mapa
# da Cena 3 · 2009 (Rede Metropolitana da RNP). Contornos oficiais do IBGE (API de
# malhas, municípios da ilha), simplificados; cores como no mapa de referência da
# equipe: São Luís em lilás, os outros municípios em cinza-claro, o mar em azul.
#
#   blender -b -P Ferramentas/blender/mapa_sao_luis.py -- [saida.fbx] [previa.png]
#
# Gera também mapa-sao-luis.json, ao lado do FBX: posições (u, v de 0 a 1 sobre o
# modelo inteiro, u para leste e v para norte) dos rótulos dos municípios e dos
# pontos da rede, que o construtor de cenas usa para os textos e os pinos.
import json
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from comum import *  # noqa: E402,F403

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
SAIDA = Path(args[0]) if args else RAIZ / "Assets/TrilhaDaInovacao/Modelos/Mapa/mapa-sao-luis.fbx"
PREVIA = Path(args[1]) if len(args) > 1 else None

URL = ("https://servicodados.ibge.gov.br/api/v3/malhas/municipios/{}"
       "?formato=application/vnd.geo%2Bjson&qualidade=maxima")
# Código IBGE -> (nome no mapa, cor do topo, altura do relevo antes da escala)
MUNICIPIOS = {
    "2111300": ("SÃO LUÍS", (0.62, 0.62, 0.95), 0.030),
    "2111201": ("SÃO JOSÉ\nDE RIBAMAR", (0.86, 0.86, 0.88), 0.024),
    "2107506": ("PAÇO DO\nLUMIAR", (0.80, 0.80, 0.83), 0.024),
    "2109452": ("RAPOSA", (0.74, 0.74, 0.78), 0.024),
}
# Rótulos fora do centroide (lon, lat), para não cair em cima de outro município.
POSICAO_ROTULO = {"2111300": (-44.29, -2.66), "2109452": (-44.10, -2.43)}
# Pontos da rede metropolitana na área urbana de São Luís (lon, lat). Os nomes das
# seis instituições ainda estão pendentes no roteiro, então os pinos vão sem rótulo.
PONTOS_REDE = [(-44.31, -2.56), (-44.29, -2.525), (-44.25, -2.50), (-44.20, -2.52), (-44.22, -2.585), (-44.28, -2.605)]

TOLERANCIA = 0.004          # graus: simplificação do contorno (low poly)
AREA_MINIMA = 0.0004        # graus²: ilhotas menores que isso ficam de fora
ESCALA = 2.0                # 1 grau = 2 m antes do ajuste no Unity
LAT0 = math.radians(-2.6)
MARGEM = 0.03               # graus de mar em volta da ilha
ESPESSURA_MAR = 0.008


def projetar(lon, lat):
    return lon * math.cos(LAT0) * ESCALA, lat * ESCALA


limpar_cena()
pecas, rotulos = [], []
for codigo, (nome, cor, altura) in MUNICIPIOS.items():
    geo = baixar_geojson(URL.format(codigo), RAIZ / f"Ferramentas/dados/municipio-{codigo}.geojson")
    g = geo["features"][0]["geometry"]
    topo = material(f"Municipio{codigo}", cor)
    lado = material(f"Borda{codigo}", tuple(c * 0.6 for c in cor))
    poligonos = g["coordinates"] if g["type"] == "MultiPolygon" else [g["coordinates"]]
    maior = None
    for k, poligono in enumerate(poligonos):
        externo = [tuple(p) for p in poligono[0]][:-1]
        if abs(area(externo)) < AREA_MINIMA:
            continue
        anel = simplificar(externo, TOLERANCIA)
        if len(anel) < 3:
            continue
        anel = [projetar(x, y) for x, y in anel]
        peca = placa_poligonal(f"{codigo}_{k}", anel, altura, topo, lado)
        peca.location.z += ESPESSURA_MAR
        pecas.append(peca)
        if maior is None or abs(area(anel)) > abs(area(maior)):
            maior = anel
    rotulos.append((nome, projetar(*POSICAO_ROTULO[codigo]) if codigo in POSICAO_ROTULO else centroide(maior)))

fixar(pecas)
ilha = juntar("Ilha", pecas)
facetar(ilha)

# Mar: placa retangular azul sob a ilha.
xs = [v.co.x for v in ilha.data.vertices]
ys = [v.co.y for v in ilha.data.vertices]
m = MARGEM * ESCALA
x0, x1, y0, y1 = min(xs) - m, max(xs) + m, min(ys) - m, max(ys) + m
mar = loft("Mar", [anel_retangular(x1 - x0, y1 - y0, 0.0, 0.0, 0, 1),
                   anel_retangular(x1 - x0, y1 - y0, 0.0, ESPESSURA_MAR, 0, 1)],
           material("Mar", (0.55, 0.80, 0.95)))
mar.location = ((x0 + x1) / 2, (y0 + y1) / 2, 0)
fixar([mar])
mapa = juntar("Mapa", [ilha, mar])
facetar(mapa)


def uv(x, y):
    return round((x - x0) / (x1 - x0), 4), round((y - y0) / (y1 - y0), 4)


pontos = [{"nome": n, "tipo": "municipio", "u": uv(*c)[0], "v": uv(*c)[1]} for n, c in rotulos]
pontos += [{"nome": f"Ponto {i + 1}", "tipo": "rede", "u": uv(*projetar(*ll))[0], "v": uv(*projetar(*ll))[1]}
           for i, ll in enumerate(PONTOS_REDE)]

# Origem no centro do mapa, no nível da mesa.
cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
for v in mapa.data.vertices:
    v.co.x -= cx
    v.co.y -= cy

paletizar("mapa-sao-luis-paleta", SAIDA.with_name("mapa-sao-luis-paleta.png"))
exportar("mapa-sl", SAIDA)
anotacao = {"proporcao": round((x1 - x0) / (y1 - y0), 4), "fonte": "IBGE, malha municipal (API v3)", "pontos": pontos}
SAIDA.with_name("mapa-sao-luis.json").write_text(json.dumps(anotacao, ensure_ascii=False, indent=1) + "\n")
print(f"[mapa-sl] rótulos: {pontos}")
if PREVIA:
    previa("mapa-sl", PREVIA, camera=(0.0, 0.0, 3.0), alvo=(0.0, 0.0, 0.0), orto=(y1 - y0) * 1.1)

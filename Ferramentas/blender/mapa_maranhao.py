# Mapa do Maranhão com as cinco mesorregiões em relevo low poly (painel da
# Cena 1 · 1986 TELMA). Contorno oficial do IBGE (API de malhas, mesorregiões),
# simplificado; cores como no mapa de referência da equipe.
#
#   blender -b -P Ferramentas/blender/mapa_maranhao.py -- [saida.fbx] [previa.png]
#
# Gera também mapa-maranhao.json, ao lado do FBX: posições (u, v de 0 a 1 sobre
# o mapa, u para leste e v para norte) dos rótulos das regiões e das cidades,
# que o construtor de cenas usa para pôr os textos e os pontos da ligação.
import json
import math
import sys
from pathlib import Path


sys.path.insert(0, str(Path(__file__).resolve().parent))
from comum import *  # noqa: E402,F403

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
SAIDA = Path(args[0]) if args else RAIZ / "Assets/TrilhaDaInovacao/Modelos/Mapa/mapa-maranhao.fbx"
PREVIA = Path(args[1]) if len(args) > 1 else None

DADOS = RAIZ / "Ferramentas/dados/maranhao-mesorregioes.geojson"
URL = ("https://servicodados.ibge.gov.br/api/v3/malhas/estados/21?intrarregiao=mesorregiao"
       "&qualidade=intermediaria&formato=application/vnd.geo%2Bjson")

# Código IBGE -> (nome no mapa, cor, altura do relevo em m antes da escala)
REGIOES = {
    "2101": ("NORTE\nMARANHENSE", (0.93, 0.70, 0.50), 0.020),
    "2102": ("OESTE\nMARANHENSE", (0.62, 0.78, 0.88), 0.014),
    "2103": ("CENTRO\nMARANHENSE", (0.80, 0.74, 0.88), 0.017),
    "2104": ("LESTE\nMARANHENSE", (0.78, 0.86, 0.50), 0.015),
    "2105": ("SUL\nMARANHENSE", (0.80, 0.68, 0.56), 0.012),
}
CIDADES = {"São Luís": (-44.30, -2.53), "Imperatriz": (-47.49, -5.52)}
# Rótulos fora do centroide (lon, lat): o do Norte iria para cima de São Luís.
POSICAO_ROTULO = {"2101": (-44.95, -3.05)}

TOLERANCIA = 0.035          # graus: simplificação do contorno (low poly)
AREA_MINIMA = 0.004         # graus²: ilhas menores que isso ficam de fora
ESCALA = 0.1                # 1 grau = 10 cm antes do ajuste no Unity
LAT0 = math.radians(-5.0)   # projeção equiretangular centrada no estado


def projetar(lon, lat):
    return lon * math.cos(LAT0) * ESCALA, lat * ESCALA


# ----------------------------------------------------------------------------
limpar_cena()
geo = baixar_geojson(URL, DADOS)

pecas, rotulos = [], []
for f in geo["features"]:
    codigo = f["properties"]["codarea"]
    nome, cor, altura = REGIOES[codigo]
    topo = material(f"Regiao{codigo}", cor)
    lado = material(f"Borda{codigo}", tuple(c * 0.6 for c in cor))
    g = f["geometry"]
    poligonos = g["coordinates"] if g["type"] == "MultiPolygon" else [g["coordinates"]]
    maior = None
    for k, poligono in enumerate(poligonos):
        externo = [tuple(p) for p in poligono[0]][:-1]           # anel externo, sem repetir o 1º ponto
        if abs(area(externo)) < AREA_MINIMA:
            continue
        anel = simplificar(externo, TOLERANCIA)
        if len(anel) < 3:
            continue
        anel = [projetar(x, y) for x, y in anel]
        pecas.append(placa_poligonal(f"{codigo}_{k}", anel, altura, topo, lado))
        if maior is None or abs(area(anel)) > abs(area(maior)):
            maior = anel
    if codigo in POSICAO_ROTULO:
        rotulos.append((nome, projetar(*POSICAO_ROTULO[codigo])))
    else:
        rotulos.append((nome, centroide(maior)))

fixar(pecas)
mapa = juntar("Mapa", pecas)
facetar(mapa)

# Limites para normalizar os rótulos (antes de deitar o mapa na parede).
xs = [v.co.x for v in mapa.data.vertices]
ys = [v.co.y for v in mapa.data.vertices]
x0, x1, y0, y1 = min(xs), max(xs), min(ys), max(ys)


def uv(x, y):
    return round((x - x0) / (x1 - x0), 4), round((y - y0) / (y1 - y0), 4)


pontos = [{"nome": n, "tipo": "regiao", "u": uv(*c)[0], "v": uv(*c)[1]} for n, c in rotulos]
pontos += [{"nome": n, "tipo": "cidade", "u": uv(*projetar(*ll))[0], "v": uv(*projetar(*ll))[1]}
           for n, ll in CIDADES.items()]

# De pé, como num painel: norte para cima (+Z) e relevo para a frente (-Y).
mapa.rotation_euler = (math.radians(90), 0, 0)
fixar([mapa])
# Origem no pé do mapa, centrado em x (o Unity reposiciona, mas facilita a prévia).
cx, cz = (x0 + x1) / 2, min(v.co.z for v in mapa.data.vertices)
for v in mapa.data.vertices:
    v.co.x -= cx
    v.co.z -= cz

paletizar("mapa-maranhao-paleta", SAIDA.with_name("mapa-maranhao-paleta.png"))
exportar("mapa", SAIDA)
anotacao = {"proporcao": round((x1 - x0) / (y1 - y0), 4), "fonte": "IBGE, malha de mesorregiões (API v3)",
            "pontos": pontos}
SAIDA.with_name("mapa-maranhao.json").write_text(json.dumps(anotacao, ensure_ascii=False, indent=1) + "\n")
print(f"[mapa] rótulos: {pontos}")
if PREVIA:
    meio = (y1 - y0) / 2
    previa("mapa", PREVIA, camera=(0.0, -2.0, meio), alvo=(0.0, 0.0, meio), orto=(y1 - y0) * 1.45)

#!/usr/bin/env python3
"""Dublagem por IA das falas do roteiro (guia robô e figurantes).

Lê Assets/TrilhaDaInovacao/Roteiro/*.asset, sintetiza cada fala com o Piper
(TTS neural local, pt-BR) e aplica uma voz robótica "Matrix" pensada para não
cansar no fone do óculos: grave, metálica e com eco curto, mas sem agudos
ásperos e com volume normalizado. Saída: Assets/TrilhaDaInovacao/Voz/<roteiro>/<id>.ogg.
O construtor de cenas liga cada arquivo ao campo "audio" da fala.

    Ferramentas/voz/gerar_vozes.py            # só o que mudou
    Ferramentas/voz/gerar_vozes.py --tudo     # regera tudo
    Ferramentas/voz/gerar_vozes.py --so Cena4_2018_Senai

Requer o Piper e as vozes pt_BR (faber, cadu, jeff) em ~/.local/opt
(ou PIPER=/caminho/piper e VOZES=/pasta/das/vozes) e o ffmpeg.
"""
import argparse
import hashlib
import json
import os
import re
import subprocess
import sys
import tempfile
from pathlib import Path

import yaml

RAIZ = Path(__file__).resolve().parents[2]
ROTEIROS = RAIZ / "Assets/TrilhaDaInovacao/Roteiro"
SAIDA = RAIZ / "Assets/TrilhaDaInovacao/Voz"
MANIFESTO = Path(__file__).resolve().parent / "manifesto.json"
PIPER = Path(os.environ.get("PIPER", Path.home() / ".local/opt/piper/piper"))
VOZES = Path(os.environ.get("VOZES", Path.home() / ".local/opt/piper-voices"))

# Mudou a cadeia de efeitos? Suba a versão para regerar tudo.
VERSAO_EFEITO = 1

# Personagens: modelo do Piper, ritmo (length_scale > 1 = mais devagar),
# tom em semitons e intensidade do efeito robótico.
PERSONAGENS = {
    "guia": dict(modelo="pt_BR-faber-medium", ritmo=1.08, tom=-2.0, robo="forte"),
    "estudante": dict(modelo="pt_BR-jeff-medium", ritmo=1.0, tom=4.0, robo="leve"),
    "pesquisador": dict(modelo="pt_BR-cadu-medium", ritmo=1.02, tom=-1.0, robo="leve"),
    "empreendedora": dict(modelo="pt_BR-cadu-medium", ritmo=0.98, tom=5.0, robo="leve"),
    "engenheiro": dict(modelo="pt_BR-jeff-medium", ritmo=1.02, tom=-2.0, robo="leve"),
}

# Quem diz cada fala que não é do guia: (roteiro, id) -> personagem.
FALANTES = {
    ("Cena4_2018_Senai", "estudante"): "estudante",
    ("Cena4_2018_Senai", "pesquisador"): "pesquisador",
    ("Cena4_2018_Senai", "empreendedora"): "empreendedora",
    ("Cena6_2023-2025_Alcantara", "causa"): "engenheiro",
    ("Cena6_2023-2025_Alcantara", "causa2"): "engenheiro",
    ("Cena6_2023-2025_Alcantara", "causa3"): "engenheiro",
    ("Cena6_2023-2025_Alcantara", "investigacao"): "engenheiro",
    ("Cena6_2023-2025_Alcantara", "licao"): "engenheiro",
}

# Falas ditas em partes: a contagem aparece número a número na legenda e cada
# número vira um arquivo (contagem-1.ogg, contagem-2.ogg...), tocado no seu segundo.
EM_PARTES = {("Cena6_2023-2025_Alcantara", "contagem")}

# Pronúncia: só para o áudio; a legenda continua com o texto original.
PRONUNCIA = [
    (r"\b1 Gbps\b", "1 gigabit por segundo"),
    (r"\b3,5 GHz\b", "três vírgula cinco gigahertz"),
    (r"\bHANBIT-TLV\b", "Hanbit tê éle vê"),
    (r"\bHANBIT-Nano\b", "Hanbit Nano"),
    (r"\bPION-BR2\b", "Píon bê érre dois"),
    (r"\bTrópico-R\b", "Trópico érre"),
    (r"\bInnospace\b", "Inospeice"),
    (r"\bSISNAV\b", "Sisnav"),
    (r"\bUFMA\b", "Ufma"),
    (r"\bINPE\b", "Inpe"),
    (r"\bCENIPA\b", "Cenipa"),
    (r"\bNASA\b", "Nasa"),
    (r"\bRNP\b", "érre ene pê"),
    (r"\bSENAI\b", "Senai"),
    (r"\bTELMA\b", "Telma"),
    (r"\b5G\b", "cinco gê"),
]


def cadeia(p):
    """Filtros do ffmpeg: tom, timbre robótico e cuidados de conforto."""
    r = 2 ** (p["tom"] / 12)
    f = [
        # Corta o silêncio que o Piper deixa no começo e no fim.
        "silenceremove=start_periods=1:start_threshold=-50dB",
        "areverse", "silenceremove=start_periods=1:start_threshold=-50dB", "areverse",
        "highpass=f=90",
        # Tom (muda também o timbre, o que ajuda a separar os personagens).
        f"asetrate=22050*{r:.5f}", "aresample=44100", f"atempo={1 / r:.5f}",
    ]
    if p["robo"] == "forte":
        f += [
            # Metal: flanger lento + pente de ecos curtíssimos; coro para a "voz digital".
            "flanger=delay=1.2:depth=1.5:regen=25:width=55:speed=0.25",
            "aecho=0.85:0.55:6|11:0.28|0.16",
            "chorus=0.7:0.9:20:0.25:0.3:1.2",
        ]
    else:
        f += [
            "aecho=0.9:0.6:6:0.14",
            "chorus=0.8:0.9:18:0.2:0.25:1.0",
        ]
    f += [
        # Conforto: sem chiado nem estridência, corpo nos graves, dinâmica contida.
        "equalizer=f=3200:t=q:w=1.0:g=-3",
        "equalizer=f=180:t=q:w=1.0:g=2",
        "lowpass=f=6500",
        "acompressor=threshold=-20dB:ratio=3:attack=8:release=150:makeup=2",
        # Sala pequena, bem discreta.
        "aecho=0.8:0.35:55|85:0.10|0.06",
        "apad=pad_dur=0.15",
        # Volume confortável e igual em todas as falas; pico bem abaixo de 0 dB.
        "loudnorm=I=-19:TP=-3:LRA=7",
    ]
    return ",".join(f)


def ler_roteiros():
    for arquivo in sorted(ROTEIROS.glob("*.asset")):
        linhas = [l for l in arquivo.read_text(encoding="utf-8").splitlines()
                  if not l.startswith("%") and not l.startswith("---")]
        dados = yaml.safe_load("\n".join(linhas))["MonoBehaviour"]
        for fala in dados.get("falas") or []:
            texto = (fala.get("texto") or "").strip()
            if texto:
                yield arquivo.stem, str(fala["id"]), texto


def para_falar(texto):
    for padrao, troca in PRONUNCIA:
        texto = re.sub(padrao, troca, texto)
    return texto


def gerar(roteiro, id_, texto, personagem, destino):
    p = PERSONAGENS[personagem]
    modelo = VOZES / f"{p['modelo']}.onnx"
    with tempfile.TemporaryDirectory() as tmp:
        bruto = Path(tmp) / "bruto.wav"
        subprocess.run([str(PIPER), "-m", str(modelo), "--length_scale", str(p["ritmo"]), "-f", str(bruto)],
                       input=para_falar(texto).encode("utf-8"), check=True, capture_output=True)
        destino.parent.mkdir(parents=True, exist_ok=True)
        subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", str(bruto), "-af", cadeia(p),
                        "-ac", "1", "-ar", "44100", "-c:a", "libvorbis", "-q:a", "5", str(destino)], check=True)


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--tudo", action="store_true", help="regera mesmo o que não mudou")
    ap.add_argument("--so", help="só este roteiro (nome do .asset, sem extensão)")
    args = ap.parse_args()

    if not PIPER.exists():
        sys.exit(f"Piper não encontrado em {PIPER} (defina PIPER=...).")

    manifesto = json.loads(MANIFESTO.read_text()) if MANIFESTO.exists() else {}
    feitas = puladas = 0
    for roteiro, id_, texto in ler_roteiros():
        if args.so and roteiro != args.so:
            continue
        personagem = FALANTES.get((roteiro, id_), "guia")
        if (roteiro, id_) in EM_PARTES:
            partes = [(f"{id_}-{i}", t.strip().rstrip(".!") + "!") for i, t in enumerate(texto.split(","), 1)]
        else:
            partes = [(id_, texto)]
        for nome, texto in partes:
            f, p = falar(args, manifesto, roteiro, nome, texto, personagem)
            feitas += f
            puladas += p

    MANIFESTO.write_text(json.dumps(manifesto, ensure_ascii=False, indent=1, sort_keys=True) + "\n")
    print(f"Vozes: {feitas} geradas, {puladas} sem mudança, em {SAIDA.relative_to(RAIZ)}")


def falar(args, manifesto, roteiro, id_, texto, personagem):
    """Gera uma fala se o texto, a voz ou o efeito mudaram. Devolve (geradas, puladas)."""
    destino = SAIDA / roteiro / f"{id_}.ogg"
    chave = f"{roteiro}/{id_}"
    assinatura = hashlib.sha1(json.dumps(
        [texto, PERSONAGENS[personagem], VERSAO_EFEITO], ensure_ascii=False, sort_keys=True).encode()).hexdigest()
    if not args.tudo and destino.exists() and manifesto.get(chave) == assinatura:
        return 0, 1
    print(f"  {chave} ({personagem}): {texto}")
    gerar(roteiro, id_, texto, personagem, destino)
    manifesto[chave] = assinatura
    return 1, 0


if __name__ == "__main__":
    main()

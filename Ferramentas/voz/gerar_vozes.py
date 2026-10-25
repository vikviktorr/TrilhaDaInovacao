#!/usr/bin/env python3
"""Dublagem por IA das falas do roteiro (guia robô e figurantes).

Lê Assets/TrilhaDaInovacao/Roteiro/*.asset, sintetiza cada fala com o Piper
(TTS neural local, pt-BR). O guia ganha uma voz de robô leve (aguda, simpática, com um toque metálico);
os figurantes ficam com voz humana. Todas passam por uma cadeia de clareza
(presença, de-esser, compressão suave) e saem com o mesmo volume (-16 LUFS,
loudnorm em duas passadas), em 48 kHz. Saída: Assets/TrilhaDaInovacao/Voz/<roteiro>/<id>.ogg.
O construtor de cenas liga cada arquivo ao campo "audio" da fala.

    Ferramentas/voz/gerar_vozes.py            # só o que mudou
    Ferramentas/voz/gerar_vozes.py --tudo     # regera tudo
    Ferramentas/voz/gerar_vozes.py --so Cena4_2018_Senai

Requer o Piper e as vozes pt_BR (faber, cadu, jeff) em ~/.local/opt
(ou PIPER=/caminho/piper e VOZES=/pasta/das/vozes) e o ffmpeg (com rubberband).
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
VERSAO_EFEITO = 3

# Volume final de todas as falas (EBU R128): -16 LUFS, o usual para fala em
# celular e fone, e pico real a -1,5 dBTP. O visitante ainda ajusta no painel do hub.
LUFS, PICO, LRA = -16.0, -1.5, 7.0   # LRA só para a medição

# Personagens: modelo do Piper, ritmo (length_scale > 1 = mais devagar),
# tom em semitons, quanto desse tom desloca também os formantes (o timbre)
# e o efeito robótico. Todas as vozes pt_BR do Piper são masculinas: para as
# mulheres, o tom sobe 5 semitons mas os formantes só 2 (como numa voz feminina
# real); subir tudo junto, como antes, dava a voz de "esquilo".
# ritmo = length_scale do Piper (maior = mais devagar). suave: fala de gente, com menos
# brilho e compressão mais leve, para soar calma e próxima (os figurantes).
PERSONAGENS = {
    # O robô é um drone pequeno e simpático: voz mais aguda, formantes um pouco mais
    # claros (soa "menor") e só um toque metálico.
    "guia": dict(modelo="pt_BR-faber-medium", ritmo=1.02, tom=3.0, formante=1.5, robo=True, suave=False),
    "estudante": dict(modelo="pt_BR-jeff-medium", ritmo=1.06, tom=4.0, formante=1.5, robo=False, suave=True),
    "pesquisador": dict(modelo="pt_BR-cadu-medium", ritmo=1.08, tom=-1.0, formante=-0.5, robo=False, suave=True),
    "empreendedora": dict(modelo="pt_BR-cadu-medium", ritmo=1.05, tom=5.0, formante=2.0, robo=False, suave=True),
    "engenheiro": dict(modelo="pt_BR-jeff-medium", ritmo=1.08, tom=-1.5, formante=-1.0, robo=False, suave=True),
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
    (r"\b10 Gbps\b", "dez gigabits por segundo"),
    (r"\b1\.008\b", "mil e oito"),
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
    """Filtros do ffmpeg antes do volume final: tom, timbre e cuidados de clareza e conforto."""
    f = [
        # Corta o silêncio que o Piper deixa no começo e no fim.
        "silenceremove=start_periods=1:start_threshold=-50dB",
        "areverse", "silenceremove=start_periods=1:start_threshold=-50dB", "areverse",
        "highpass=f=80",
        # Trabalha em 48 kHz com o reamostrador soxr (o padrão do ffmpeg deixa chiado).
        "aresample=48000:resampler=soxr",
    ]
    if p["formante"]:
        # Muda tom e formantes juntos (velocidade), depois devolve a duração.
        r = 2 ** (p["formante"] / 12)
        f += [f"asetrate={48000 * r:.1f}", "aresample=48000:resampler=soxr", f"atempo={1 / r:.5f}"]
    resto = p["tom"] - p["formante"]
    if resto:
        # O resto do tom sem mexer nos formantes.
        f.append(f"rubberband=pitch={2 ** (resto / 12):.5f}:formant=preserved:pitchq=quality")
    if p["robo"]:
        # Voz de robô leve e simpática: um só eco curtíssimo (brilho metálico) e um coro
        # sutil. Ecos e coro fortes embolavam as sílabas e deixavam a voz dura.
        f += ["aecho=0.9:0.7:3:0.12", "chorus=0.9:0.9:12:0.1:0.3:0.8"]
    if p["suave"]:
        # Gente: corpo nos graves, quase nada de presença, agudos arredondados e um
        # compressor gentil; a fala fica calma sem perder a clareza.
        f += [
            "equalizer=f=220:t=q:w=1.2:g=1",
            "equalizer=f=2800:t=q:w=1.4:g=0.8",
            "treble=g=-3:f=5500",
            "deesser=i=0.55",
            "lowpass=f=9000",
            "acompressor=threshold=-24dB:ratio=1.8:attack=12:release=200:makeup=1.5",
        ]
    else:
        f += [
            # Clareza: menos "embolado" nos médios-graves, presença onde a fala é entendida,
            # sem chiado de "s" nem agudos ásperos no fone do óculos.
            "equalizer=f=280:t=q:w=1.2:g=-2",
            "equalizer=f=2800:t=q:w=1.4:g=2",
            "treble=g=-1.5:f=7000",
            "deesser=i=0.45",
            "lowpass=f=10500",
            "acompressor=threshold=-22dB:ratio=2.2:attack=8:release=150:makeup=2",
        ]
    f.append("apad=pad_dur=0.12")
    return ",".join(f)


def medir(arquivo):
    """1ª passada do loudnorm: mede o volume da fala já processada."""
    saida = subprocess.run(["ffmpeg", "-hide_banner", "-nostats", "-i", str(arquivo), "-af",
                            f"loudnorm=I={LUFS}:TP={PICO}:LRA={LRA}:print_format=json", "-f", "null", "-"],
                           check=True, capture_output=True, text=True).stderr
    return json.loads(saida[saida.rindex("{"):saida.rindex("}") + 1])


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
        tratado = Path(tmp) / "tratado.wav"
        subprocess.run([str(PIPER), "-m", str(modelo), "--length_scale", str(p["ritmo"]), "-f", str(bruto)],
                       input=para_falar(texto).encode("utf-8"), check=True, capture_output=True)
        subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", str(bruto), "-af", cadeia(p),
                        "-ac", "1", "-c:a", "pcm_f32le", str(tratado)], check=True)
        # 2ª passada: ganho exato para o mesmo volume em todas as falas (o loudnorm de
        # uma passada deixava até 3 dB de diferença) e um limitador que segura os picos.
        m = medir(tratado)
        ganho = LUFS - float(m["input_i"])
        volume = f"volume={ganho:.2f}dB,alimiter=limit={10 ** (PICO / 20):.3f}:attack=3:release=60:level=false"
        destino.parent.mkdir(parents=True, exist_ok=True)
        subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", str(tratado), "-af", f"{volume},aresample=48000:resampler=soxr",
                        "-ac", "1", "-ar", "48000", "-c:a", "libvorbis", "-q:a", "6", str(destino)], check=True)


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

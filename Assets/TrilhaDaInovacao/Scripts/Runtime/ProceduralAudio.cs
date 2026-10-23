using System.Collections.Generic;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Efeitos sonoros gerados por código, para o whitebox ter som sem
    /// depender de arquivos. Cada som é criado uma vez e reaproveitado.
    /// Trocar por gravações reais é só apontar um AudioClip no lugar.
    /// </summary>
    public static class ProceduralAudio
    {
        const int Taxa = 44100;

        static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

        public static AudioClip Obter(string nome)
        {
            if (cache.TryGetValue(nome, out var clip) && clip != null) return clip;

            clip = nome switch
            {
                "discagem" => Gerar(nome, 1.6f, t => 0.35f * Seno(425f, t) * Envelope(t, 1.6f, 0.02f, 0.05f)),
                "toque" => Gerar(nome, 2f, Toque),
                "chime" => Gerar(nome, 0.7f, t =>
                    0.4f * (Seno(880f, t) + 0.5f * Seno(1320f, t)) * Mathf.Exp(-t * 6f)),
                "clique" => Gerar(nome, 0.08f, t => 0.5f * Seno(1800f, t) * Mathf.Exp(-t * 60f)),
                "bipe" => Gerar(nome, 0.45f, t => 0.45f * Seno(1250f, t) * Mathf.Exp(-t * 9f)),
                "contagem" => Gerar(nome, 0.25f, t => 0.45f * Seno(990f, t) * Envelope(t, 0.25f, 0.005f, 0.08f)),
                "pulso-grave" => Gerar(nome, 0.9f, t => 0.7f * Seno(55f + 30f * Mathf.Exp(-t * 4f), t) * Mathf.Exp(-t * 3.5f)),
                "zumbido" => Gerar(nome, 2f, t => 0.12f * (Seno(60f, t) + 0.5f * Seno(120f, t) + 0.25f * Seno(180f, t))),
                "vento" => Ruido(nome, 4f, 0.015f, 0.25f, true),
                "servidores" => Ruido(nome, 3f, 0.12f, 0.12f, false),
                "oficina" => Ruido(nome, 3f, 0.05f, 0.1f, true),
                "cidade" => Ruido(nome, 4f, 0.02f, 0.15f, true),
                "motor" => Ruido(nome, 3f, 0.008f, 1f, false),
                "impressora" => Gerar(nome, 2f, t =>
                    0.15f * Seno(220f + 40f * Mathf.Sin(t * 6f * Mathf.PI), t)),
                "whoosh" => Whoosh(nome),
                "explosao" => Explosao(nome),
                "sirene" => Gerar(nome, 2.4f, t =>
                    0.35f * Seno(600f + 300f * Mathf.Sin(t * Mathf.PI), t) * Envelope(t, 2.4f, 0.05f, 0.3f)),
                _ => null
            };

            if (clip == null) Debug.LogWarning($"[Trilha da Inovação] Som '{nome}' não existe.");
            cache[nome] = clip;
            return clip;
        }

        static float Seno(float frequencia, float t) => Mathf.Sin(2f * Mathf.PI * frequencia * t);

        static float Envelope(float t, float duracao, float ataque, float soltura)
        {
            if (t < ataque) return t / ataque;
            if (t > duracao - soltura) return Mathf.Max(0f, (duracao - t) / soltura);
            return 1f;
        }

        /// <summary>Campainha eletromecânica: duas notas batendo a 20 Hz, 1 s ligada.</summary>
        static float Toque(float t)
        {
            if (t > 1f) return 0f;
            var batida = Mathf.Sin(2f * Mathf.PI * 20f * t) > 0f ? 1f : 0.3f;
            return 0.3f * batida * (Seno(440f, t) + 0.7f * Seno(480f, t)) * Envelope(t, 1f, 0.01f, 0.05f);
        }

        static AudioClip Gerar(string nome, float duracao, System.Func<float, float> amostra)
        {
            var total = Mathf.CeilToInt(duracao * Taxa);
            var dados = new float[total];
            for (var i = 0; i < total; i++) dados[i] = Mathf.Clamp(amostra(i / (float)Taxa), -1f, 1f);

            var clip = AudioClip.Create(nome, total, 1, Taxa, false);
            clip.SetData(dados, 0);
            return clip;
        }

        /// <summary>
        /// Ruído filtrado (passa-baixa de um polo). "suave" modula o volume
        /// devagar, como rajadas de vento. O fim emenda no começo para loop.
        /// </summary>
        static AudioClip Ruido(string nome, float duracao, float corte, float volume, bool suave)
        {
            var total = Mathf.CeilToInt(duracao * Taxa);
            var dados = new float[total];
            var rng = new System.Random(nome.GetHashCode());
            var filtrado = 0f;

            for (var i = 0; i < total; i++)
            {
                var branco = (float)(rng.NextDouble() * 2.0 - 1.0);
                filtrado += corte * (branco - filtrado);
                var t = i / (float)Taxa;
                var modulacao = suave ? 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * t / duracao) : 1f;
                dados[i] = filtrado * modulacao;
            }

            // Normaliza e cruza as pontas para o loop não estalar.
            var pico = 0.0001f;
            foreach (var d in dados) pico = Mathf.Max(pico, Mathf.Abs(d));
            var emenda = Mathf.Min(total / 4, Taxa / 4);
            for (var i = 0; i < total; i++) dados[i] = dados[i] / pico * volume;
            for (var i = 0; i < emenda; i++)
            {
                var a = i / (float)emenda;
                dados[i] = dados[i] * a + dados[total - emenda + i] * (1f - a);
            }

            var clip = AudioClip.Create(nome, total - emenda, 1, Taxa, false);
            var final = new float[total - emenda];
            System.Array.Copy(dados, final, final.Length);
            clip.SetData(final, 0);
            return clip;
        }

        /// <summary>
        /// Estrondo distante: estalo inicial, ruído grave que vai se fechando e um
        /// ronco de 38 Hz; pico baixo para não doer no fone do Quest.
        /// </summary>
        static AudioClip Explosao(string nome)
        {
            const float duracao = 4f;
            var total = Mathf.CeilToInt(duracao * Taxa);
            var dados = new float[total];
            var rng = new System.Random(22);
            var filtrado = 0f;

            for (var i = 0; i < total; i++)
            {
                var t = i / (float)Taxa;
                var corte = Mathf.Lerp(0.25f, 0.012f, Mathf.Clamp01(t / 1.2f));
                filtrado += corte * ((float)(rng.NextDouble() * 2.0 - 1.0) - filtrado);
                var envelope = Mathf.Min(1f, t / 0.01f) * Mathf.Exp(-t * 1.1f);
                var ronco = 0.35f * Seno(38f, t) * Mathf.Exp(-t * 0.8f);
                dados[i] = 0.75f * (filtrado * 3f + ronco) * envelope;
            }

            var clip = AudioClip.Create(nome, total, 1, Taxa, false);
            clip.SetData(dados, 0);
            return clip;
        }

        static AudioClip Whoosh(string nome)
        {
            const float duracao = 1.2f;
            var total = Mathf.CeilToInt(duracao * Taxa);
            var dados = new float[total];
            var rng = new System.Random(7);
            var filtrado = 0f;

            for (var i = 0; i < total; i++)
            {
                var t = i / (float)Taxa;
                var corte = Mathf.Lerp(0.01f, 0.25f, Mathf.Sin(Mathf.PI * t / duracao));
                filtrado += corte * ((float)(rng.NextDouble() * 2.0 - 1.0) - filtrado);
                dados[i] = filtrado * 1.2f * Mathf.Sin(Mathf.PI * t / duracao);
            }

            var clip = AudioClip.Create(nome, total, 1, Taxa, false);
            clip.SetData(dados, 0);
            return clip;
        }
    }
}

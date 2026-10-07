using System.Collections;
using TMPro;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>Cena 3 — 2009, Rede Metropolitana de São Luís (1:35–2:10).</summary>
    public class Cena3Fibra : EraSequence
    {
        public PokeTrigger ponto;
        public Pulsar brilhoPonto;
        public LinhaProgressiva[] feixes;
        public Renderer[] pinos;
        public Color corPino = new Color(0.3f, 1f, 1f);
        public TMP_Text contador;
        public AudioSource ambiente;
        public AudioSource sons;

        const float KmTotal = 43f;

        protected override IEnumerator Rodar()
        {
            Cena1Telma.Tocar(ambiente, "servidores", true, 0.35f);
            contador.text = "0 km";
            foreach (var f in feixes) f.Esconder();
            brilhoPonto.Definir(false);

            yield return Dizer("intro");
            brilhoPonto.Definir(true);
            ctx.Enquadrar(ponto.transform.position);
            yield return Dizer("numeros");

            yield return EsperarToque(ponto, "dica");
            brilhoPonto.Definir(false);

            var rede = StartCoroutine(Conectar());
            yield return Dizer("veja");
            yield return rede;
            ctx.Capturar("2009");
            yield return Dizer("fim");
        }

        IEnumerator Conectar()
        {
            for (var i = 0; i < feixes.Length; i++)
            {
                yield return feixes[i].Desenhar(0.8f);
                if (i < pinos.Length)
                {
                    var m = pinos[i].material;
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", corPino * 2f);
                }

                sons.PlayOneShot(ProceduralAudio.Obter("chime"), 0.5f);
                contador.text = $"{Mathf.RoundToInt(KmTotal * (i + 1) / feixes.Length)} km";
            }

            contador.text = "43 km · 1 Gbps";
        }
    }
}

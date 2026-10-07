using System.Collections;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>Cena 5 — 2022, 5G em 3,5 GHz em São Luís (2:45–3:20).</summary>
    public class Cena5Rede5G : EraSequence
    {
        public ProximityTrigger torre;
        public OndasEmAnel ondasNoMapa;
        public OndasEmAnel ondasNaCidade;
        public GameObject painelComparacao;
        public Transform[] barras;
        public AudioSource ambiente;

        protected override IEnumerator Rodar()
        {
            Cena1Telma.Tocar(ambiente, "cidade", true, 0.4f);
            ondasNoMapa.ativo = false;
            ondasNaCidade.ativo = false;
            Mostrar(painelComparacao, false);

            yield return Dizer("intro");
            ctx.Enquadrar(torre.transform.position + Vector3.up * 1.5f);

            yield return EsperarProximidade(torre, "dica");
            ondasNoMapa.Iniciar();
            ondasNaCidade.Iniciar();
            yield return Dizer("anatel");

            Mostrar(painelComparacao, true);
            ctx.Enquadrar(painelComparacao.transform.position);
            StartCoroutine(CrescerBarras(2.5f));
            yield return Dizer("expansao");
            ctx.Capturar("2022");
            yield return Dizer("fim");
        }

        IEnumerator CrescerBarras(float duracao)
        {
            var finais = new Vector3[barras.Length];
            for (var i = 0; i < barras.Length; i++)
            {
                finais[i] = barras[i].localScale;
                barras[i].localScale = new Vector3(finais[i].x, 0.001f, finais[i].z);
            }

            for (var t = 0f; t < duracao; t += Time.deltaTime)
            {
                for (var i = 0; i < barras.Length; i++)
                {
                    var inicio = i * 0.25f;
                    var k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t / duracao - inicio) / (1f - inicio)));
                    barras[i].localScale = new Vector3(finais[i].x, Mathf.Max(0.001f, finais[i].y * k), finais[i].z);
                }

                yield return null;
            }

            for (var i = 0; i < barras.Length; i++) barras[i].localScale = finais[i];
        }
    }
}

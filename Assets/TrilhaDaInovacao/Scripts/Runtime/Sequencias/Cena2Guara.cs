using System.Collections;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>Cena 2 — 1994, campanha Guará em Alcântara (1:00–1:35).</summary>
    public class Cena2Guara : EraSequence
    {
        [Tooltip("Botão de ligar o radar, na mesa do console do operador.")]
        public PokeTrigger botaoRadar;
        public Pulsar brilhoBotao;
        [Tooltip("Fósforo da PPI, eco e traço da A-scope: acendem ao ligar.")]
        public GameObject telaLigada;
        public Girar varredura;
        public Girar antena;
        public GameObject feixe;
        public Transform ionosfera;
        public GameObject rotuloIonosfera;
        public LinhaProgressiva trajetoria;
        public Transform pontoNoCeu;
        public AudioSource ambiente;
        public AudioSource bipe;

        protected override IEnumerator Rodar()
        {
            Cena1Telma.Tocar(ambiente, "vento", true, 0.6f);
            antena.ativo = false;
            Mostrar(feixe, false);
            Mostrar(ionosfera.gameObject, false);
            Mostrar(rotuloIonosfera, false);
            Mostrar(telaLigada, false);
            varredura.ativo = false;
            brilhoBotao.Definir(false);

            yield return Dizer("intro");
            brilhoBotao.Definir(true);
            ctx.Enquadrar(botaoRadar.transform.position);
            yield return Dizer("pesquisadores");

            yield return EsperarToque(botaoRadar, "dica");
            brilhoBotao.Definir(false);

            // A tela PPI acende e a varredura gira junto com a antena lá fora.
            Mostrar(telaLigada, true);
            varredura.ativo = true;
            antena.ativo = true;
            Mostrar(feixe, true);
            StartCoroutine(Bipar());
            ctx.Enquadrar(varredura.transform.position);
            yield return null;
            ctx.Capturar("1994-radar");
            yield return null;
            StartCoroutine(Surgir(ionosfera, 1.5f));
            Mostrar(rotuloIonosfera, true);
            StartCoroutine(trajetoria.Desenhar(5f));
            ctx.Enquadrar(pontoNoCeu.position);

            yield return Dizer("ionosfera");
            ctx.Capturar("1994");
            yield return Dizer("fim");
        }

        IEnumerator Bipar()
        {
            while (true)
            {
                bipe.PlayOneShot(ProceduralAudio.Obter("bipe"), 0.6f);
                yield return new WaitForSeconds(360f / Mathf.Max(1f, antena.grausPorSegundo));
            }
        }
    }
}

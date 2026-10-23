using System.Collections;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Cena 2 — 1994, campanha Guará em Alcântara (1:00–1:35). O visitante liga o
    /// radar CUPRI, de antenas fixas apontadas para o céu: os pulsos sobem até a
    /// ionosfera e a tela desenha o mapa RTI (altura × hora) com os ecos.
    /// </summary>
    public class Cena2Guara : EraSequence
    {
        [Tooltip("Botão de ligar o radar, na mesa do console do operador.")]
        public PokeTrigger botaoRadar;
        public Pulsar brilhoBotao;
        [Tooltip("Mapa RTI e traço da A-scope: acendem ao ligar.")]
        public GameObject telaLigada;
        public RevelarRTI rti;
        [Tooltip("Pulsos que sobem dos arranjos de antenas; tocam o bipe.")]
        public PulsosRadar pulsos;
        public GameObject feixe;
        public Transform ionosfera;
        public GameObject rotuloIonosfera;
        public LinhaProgressiva trajetoria;
        public Transform pontoNoCeu;
        public AudioSource ambiente;

        protected override IEnumerator Rodar()
        {
            Cena1Telma.Tocar(ambiente, "vento", true, 0.6f);
            pulsos.ativo = false;
            Mostrar(feixe, false);
            Mostrar(ionosfera.gameObject, false);
            Mostrar(rotuloIonosfera, false);
            Mostrar(telaLigada, false);
            rti.ativo = false;
            brilhoBotao.Definir(false);

            yield return Dizer("intro");
            brilhoBotao.Definir(true);
            ctx.Enquadrar(botaoRadar.transform.position);
            yield return Dizer("pesquisadores");

            yield return EsperarToque(botaoRadar, "dica");
            brilhoBotao.Definir(false);

            // O transmissor liga: os pulsos sobem pelo feixe e o mapa RTI começa a ser desenhado.
            Mostrar(telaLigada, true);
            rti.Iniciar();
            pulsos.Iniciar();
            Mostrar(feixe, true);
            ctx.Enquadrar(rti.transform.position);
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

        public override void AoInterromper()
        {
            base.AoInterromper();
            // O bipe vem dos pulsos, não de uma coroutine desta cena.
            pulsos.ativo = false;
        }
    }
}

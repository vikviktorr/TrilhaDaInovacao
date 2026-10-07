using System.Collections;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>Cena 4 — 2018, Laboratório Aberto do SENAI (2:10–2:45).</summary>
    public class Cena4Senai : EraSequence
    {
        public PokeTrigger lampada;
        public Pulsar brilhoLampada;
        public LinhaProgressiva desenhoDigital;
        public ImpressaoEmCamadas impressora;
        public Transform prototipo;
        public AudioSource somImpressora;
        public AudioSource ambiente;

        protected override IEnumerator Rodar()
        {
            Cena1Telma.Tocar(ambiente, "oficina", true, 0.35f);
            desenhoDigital.Esconder();
            impressora.Esconder();
            Mostrar(prototipo.gameObject, false);
            brilhoLampada.Definir(false);

            yield return Dizer("intro");
            brilhoLampada.Definir(true);
            ctx.Enquadrar(lampada.transform.position);
            yield return Dizer("juntos");

            yield return EsperarToque(lampada, "dica");
            brilhoLampada.Definir(false);

            var fabricacao = StartCoroutine(Fabricar());
            yield return Dizer("ideia");
            yield return fabricacao;
            ctx.Capturar("2018");
            yield return Dizer("fim");
        }

        IEnumerator Fabricar()
        {
            ctx.Enquadrar(desenhoDigital.transform.position);
            yield return desenhoDigital.Desenhar(2.5f);

            ctx.Enquadrar(impressora.transform.position);
            Cena1Telma.Tocar(somImpressora, "impressora", true, 0.5f);
            yield return impressora.Imprimir(4f);
            somImpressora.Stop();

            impressora.Esconder();
            yield return Surgir(prototipo, 0.6f);
        }
    }
}

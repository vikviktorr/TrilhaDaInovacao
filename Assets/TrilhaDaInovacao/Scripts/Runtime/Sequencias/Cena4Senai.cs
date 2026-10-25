using System.Collections;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>Cena 4 — 2018, Laboratório Aberto do SENAI (2:10–2:45).</summary>
    public class Cena4Senai : EraSequence
    {
        [Tooltip("Tela do computador do CAD: tocar nela começa o desenho da peça.")]
        public PokeTrigger telaCad;
        public Pulsar brilhoTela;
        public LinhaProgressiva desenhoDigital;
        public ImpressaoEmCamadas impressora;
        [Tooltip("Painel LCD da impressora: como na Ender-3, a impressão começa ali.")]
        public PokeTrigger painelImpressora;
        public Transform prototipo;
        public AudioSource somImpressora;
        public AudioSource ambiente;

        [Header("Figurantes que falam")]
        public Figurante estudante;
        public Figurante pesquisador;
        public Figurante empreendedora;

        protected override IEnumerator Rodar()
        {
            Cena1Telma.Tocar(ambiente, "oficina", true, 0.35f);
            desenhoDigital.Esconder();
            impressora.Esconder();
            Mostrar(prototipo.gameObject, false);
            brilhoTela.Definir(false);

            yield return Dizer("intro");
            brilhoTela.Definir(true);
            ctx.Enquadrar(telaCad.transform.position);
            yield return Dizer("juntos");
            ctx.Enquadrar(estudante.transform.position + Vector3.up * 0.7f);
            yield return null;
            ctx.Capturar("2018-estudante");
            yield return null;
            yield return DizerPor(estudante, "estudante");

            yield return EsperarToque(telaCad, "dica");
            brilhoTela.Definir(false);

            ctx.Enquadrar(desenhoDigital.transform.position);
            var desenho = StartCoroutine(desenhoDigital.Desenhar(2.5f));
            yield return Dizer("ideia");
            yield return DizerPor(pesquisador, "pesquisador");
            yield return desenho;

            // O visitante dá a ordem de imprimir no próprio painel da impressora.
            ctx.Enquadrar(painelImpressora.transform.position);
            yield return EsperarToque(painelImpressora, "dica-impressora");
            yield return Imprimir();
            yield return DizerPor(empreendedora, "empreendedora");
            ctx.Capturar("2018");
            yield return Dizer("fim");
        }

        IEnumerator Imprimir()
        {
            ctx.Enquadrar(impressora.transform.position);
            Cena1Telma.Tocar(somImpressora, "impressora", true, 0.5f);
            yield return impressora.Imprimir(4f);
            somImpressora.Stop();

            impressora.Esconder();
            yield return Surgir(prototipo, 0.6f);
        }
    }
}

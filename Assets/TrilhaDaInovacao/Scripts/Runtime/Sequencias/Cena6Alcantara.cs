using System.Collections;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Cena 6 — 2023 a 2025, lançamentos em Alcântara (3:20–4:20).
    /// Parte A: interior do HANBIT-TLV com o SISNAV (2023).
    /// Parte B: plataforma do HANBIT-Nano, contagem, decolagem e resultado (2025).
    /// </summary>
    public class Cena6Alcantara : EraSequence
    {
        public GameObject parte2023;
        public GameObject parte2025;
        public Pulsar sisnav;
        public Transform posicaoGuia2025;
        public Decolagem decolagem;
        public Transform painelResultado;
        public GameObject avisoReconstituicao;
        public AudioSource ambiente;

        protected override IEnumerator Rodar()
        {
            Mostrar(parte2023, true);
            Mostrar(parte2025, false);
            Mostrar(painelResultado.gameObject, false);
            Mostrar(avisoReconstituicao, false);
            sisnav.Definir(false);

            yield return Dizer("intro2023");
            sisnav.Definir(true);
            ctx.Enquadrar(sisnav.transform.position);
            yield return Dizer("sisnav");
            ctx.Capturar("2023-sisnav");

            // Segunda virada de época dentro da mesma cena.
            ctx.Som("whoosh", 0.6f);
            yield return ctx.Fader.Fechar(Color.white);
            ctx.Fader.MostrarAno("2025");
            Mostrar(parte2023, false);
            Mostrar(parte2025, true);
            PosicionarGuia(posicaoGuia2025);
            Cena1Telma.Tocar(ambiente, "vento", true, 0.5f);
            yield return Esperar(ctx.Fader.TempoDoAno);
            yield return ctx.Fader.Abrir();

            Mostrar(avisoReconstituicao, true);
            ctx.Enquadrar(decolagem.foguete.position + Vector3.up * 8f);
            yield return Dizer("intro2025");

            // O guia é interrompido no meio da fala sobre as cargas.
            StartCoroutine(Dizer("pion"));
            yield return Esperar(ctx.Tour ? 0.8f : 2.4f);
            ctx.Som("sirene", 0.5f);
            yield return Dizer("decolar");

            yield return Contagem();
            var subida = StartCoroutine(decolagem.Subir(11f));
            StartCoroutine(AcompanharNoTour());
            yield return Esperar(2.2f);
            ctx.Capturar("2025-decolagem");
            yield return subida;

            yield return Surgir(painelResultado, 0.6f);
            ctx.Enquadrar(painelResultado.position);
            yield return Dizer("resultado");
            ctx.Capturar("2025-resultado");
            yield return Dizer("reconstituicao");
        }

        IEnumerator Contagem()
        {
            var numeros = roteiro.Obter("contagem").texto.Split(',');
            for (var i = 0; i < numeros.Length; i++)
            {
                ctx.Legendar(numeros[i].Trim().TrimEnd('.', '!'));
                ctx.Som("contagem", 0.8f);
                if (i == numeros.Length - 2) StartCoroutine(decolagem.Ignicao(1.6f));
                yield return Esperar(1f);
            }

            ctx.Legendar(string.Empty);
            ctx.Guia.Falando = false;
        }

        IEnumerator AcompanharNoTour()
        {
            while (decolagem.foguete.gameObject.activeSelf)
            {
                ctx.Enquadrar(decolagem.foguete.position);
                yield return null;
            }
        }

        public override void AoInterromper()
        {
            base.AoInterromper();
            decolagem.Parar();
        }
    }
}

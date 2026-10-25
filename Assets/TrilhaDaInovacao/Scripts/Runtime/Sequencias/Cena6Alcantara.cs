using System.Collections;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Cena 6 — 2023 a 2025, lançamentos em Alcântara (3:20–4:20).
    /// Parte A: interior do HANBIT-TLV com o SISNAV (2023).
    /// Parte B: plataforma do HANBIT-Nano, contagem e o voo de 22/12/2025 como foi:
    /// ruptura aos 33 s, queda em pedaços e explosão na zona de segurança (ver <see cref="Decolagem"/>).
    /// </summary>
    public class Cena6Alcantara : EraSequence
    {
        public GameObject parte2023;
        public GameObject parte2025;
        public Pulsar sisnav;
        public Transform posicaoGuia2025;
        public Decolagem decolagem;
        [Tooltip("Engenheiro no mirante que explica a falha do HANBIT-Nano.")]
        public Figurante engenheiro;
        public Transform painelResultado;
        public GameObject avisoReconstituicao;
        public AudioSource ambiente;
        [Tooltip("Voz do guia para cada número da contagem, na ordem (Voz/<roteiro>/contagem-N.ogg).")]
        public AudioClip[] vozContagem;
        [Tooltip("Placar do relógio de contagem na área de observação.")]
        public TMPro.TMP_Text relogio;

        protected override IEnumerator Rodar()
        {
            Mostrar(parte2023, true);
            Mostrar(parte2025, false);
            Mostrar(painelResultado.gameObject, false);
            Mostrar(avisoReconstituicao, false);
            sisnav.Definir(false);
            if (relogio != null) relogio.text = "T-00:10";

            yield return Dizer("intro2023");
            sisnav.Definir(true);
            ctx.Enquadrar(sisnav.transform.position);
            yield return Dizer("sisnav");
            ctx.Capturar("2023-sisnav");

            // Segunda virada de época dentro da mesma cena.
            yield return ctx.EntrarNoTunel("2025");
            Mostrar(parte2023, false);
            Mostrar(parte2025, true);
            PosicionarGuia(posicaoGuia2025);
            Cena1Telma.Tocar(ambiente, "vento", true, 0.5f);
            yield return ctx.SairDoTunel();
            yield return ctx.Fader.Abrir();

            Mostrar(avisoReconstituicao, true);
            // Da apresentação do foguete até a explosão o visitante olha para o
            // alto; a legenda sobe junto para a contagem e a anomalia não sumirem.
            ctx.Legenda.SeguirInclinacao = true;
            ctx.Enquadrar(decolagem.foguete.position + Vector3.up * 11f);
            yield return Dizer("intro2025");

            // O guia é interrompido no meio da fala sobre as cargas.
            StartCoroutine(Dizer("pion"));
            yield return Esperar(ctx.Tour && !ctx.QA ? 0.8f : 2.4f);
            ctx.Som("sirene", 0.5f);
            yield return Dizer("decolar");

            decolagem.Escala = ctx.Tour && !ctx.QA ? 6f : 1f;
            yield return Contagem();

            // O voo real: subida, nuvens, Max-Q, ruptura aos 33 s, queda e explosão no solo.
            var fase = Decolagem.Fase.Subindo;
            var voo = StartCoroutine(decolagem.Voar(f => fase = f));
            StartCoroutine(Placar());
            StartCoroutine(AcompanharNoTour());
            StartCoroutine(CapturarDepois("2025-decolagem", 2.2f));

            while (fase < Decolagem.Fase.Anomalia) yield return null;
            StartCoroutine(CapturarDepois("2025-anomalia", 0.2f));
            StartCoroutine(Dizer("anomalia"));

            while (fase < Decolagem.Fase.Caindo) yield return null;
            yield return Esperar(ctx.Tour && !ctx.QA ? 0.5f : 5f);
            StartCoroutine(Dizer("queda"));

            while (fase < Decolagem.Fase.Impacto) yield return null;
            ctx.Enquadrar(decolagem.PontoDaExplosao);
            yield return null;
            ctx.Capturar("2025-explosao");
            yield return voo;
            ctx.Enquadrar(decolagem.PontoDaExplosao);
            ctx.Legenda.SeguirInclinacao = false;

            yield return Dizer("resultado");
            ctx.Enquadrar(engenheiro != null ? engenheiro.transform.position + Vector3.up * 1.6f : painelResultado.position);
            yield return DizerPor(engenheiro, "causa");
            yield return DizerPor(engenheiro, "causa2");
            yield return DizerPor(engenheiro, "causa3");
            yield return DizerPor(engenheiro, "investigacao");
            yield return DizerPor(engenheiro, "licao");

            yield return Surgir(painelResultado, 0.6f);
            ctx.Enquadrar(painelResultado.position);
            yield return Dizer("resultado2");
            ctx.Capturar("2025-resultado");
            yield return Dizer("reconstituicao");
        }

        IEnumerator Contagem()
        {
            var numeros = roteiro.Obter("contagem").texto.Split(',');
            for (var i = 0; i < numeros.Length; i++)
            {
                var numero = numeros[i].Trim().TrimEnd('.', '!');
                ctx.Legendar(char.ToUpper(numero[0]) + numero.Substring(1));
                if (relogio != null) relogio.text = $"T-00:0{numeros.Length - i}";
                var voz = vozContagem != null && i < vozContagem.Length ? vozContagem[i] : null;
                if (voz != null && ctx.Guia.Voz != null)
                {
                    ctx.Guia.Falando = true;
                    ctx.Guia.Voz.PlayOneShot(voz);
                }
                else
                {
                    ctx.Som("contagem", 0.8f);
                }

                if (i == numeros.Length - 2) StartCoroutine(decolagem.Ignicao(1.6f));
                yield return Esperar(1f);
            }

            ctx.Legendar(string.Empty);
            ctx.Guia.Falando = false;
            if (relogio != null) relogio.text = "T+00:00";
        }

        IEnumerator CapturarDepois(string nome, float segundos)
        {
            yield return Esperar(segundos);
            ctx.Capturar(nome);
        }

        IEnumerator AcompanharNoTour()
        {
            while (decolagem.Estado != Decolagem.Fase.Fim && decolagem.Estado != Decolagem.Fase.Parado)
            {
                ctx.Enquadrar(decolagem.PontoAtual);
                yield return null;
            }
        }

        /// <summary>
        /// Relógio da missão como na transmissão: T+ em tempo real, "MAX-Q" aos 30 s e,
        /// na ruptura, "ANOMALIA EM VOO", com o relógio parado no último dado recebido.
        /// </summary>
        IEnumerator Placar()
        {
            if (relogio == null) yield break;
            while (decolagem.Estado == Decolagem.Fase.Subindo)
            {
                var t = decolagem.TempoDeVoo;
                var aviso = t >= decolagem.tempoMaxQ ? "\n<size=35%>MAX-Q</size>" : "";
                relogio.text = $"T+00:{Mathf.FloorToInt(t):00}{aviso}";
                yield return null;
            }

            relogio.text = $"T+00:{Mathf.FloorToInt(decolagem.tempoRuptura):00}\n<size=35%>ANOMALIA EM VOO</size>";
        }

        public override void AoInterromper()
        {
            base.AoInterromper();
            decolagem.Parar();
        }
    }
}

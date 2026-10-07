using System.Collections;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>Cena 7 — encerramento no hub, com o visitante na marca 6 (4:20–4:45).</summary>
    public class Cena7Encerramento : EraSequence
    {
        public LinhaDoTempoHub linhaDoTempo;
        public GameObject creditos;
        public GameObject botaoComecar;
        public AudioSource sons;

        protected override IEnumerator Rodar()
        {
            Mostrar(botaoComecar, false);
            linhaDoTempo.ApagarTodos();
            ctx.Enquadrar(linhaDoTempo.transform.position);
            var acender = StartCoroutine(linhaDoTempo.AcenderPontos(0.45f, sons));

            yield return Dizer("historia");
            yield return acender;
            ctx.Capturar("encerramento-linha");

            Mostrar(creditos, true);
            ctx.Enquadrar(creditos.transform.position);
            yield return Dizer("obrigado");
            ctx.Capturar("encerramento-creditos");
            yield return Dizer("retirar");
        }
    }
}

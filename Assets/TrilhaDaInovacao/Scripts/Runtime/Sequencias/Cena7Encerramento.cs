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
        [Tooltip("Painel de volume da abertura: escondido no fim (poupa ~30 draw calls perto dos créditos).")]
        public GameObject painelVolume;

        protected override IEnumerator Rodar()
        {
            Mostrar(botaoComecar, false);
            Mostrar(painelVolume, false);
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

using System.Collections;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>Cena 0 — abertura no hub do Sebraelab (0:00–0:25).</summary>
    public class Cena0Abertura : EraSequence
    {
        public LinhaDoTempoHub linhaDoTempo;
        public PokeTrigger botaoComecar;
        public GameObject creditos;
        [Tooltip("Painel de volume (vozes e efeitos), ao alcance de quem está na marca de início.")]
        public GameObject painelVolume;

        protected override IEnumerator Rodar()
        {
            linhaDoTempo.ApagarTodos();
            Mostrar(creditos, false);
            Mostrar(painelVolume, true);
            Mostrar(botaoComecar.gameObject, false);

            yield return Dizer("ola");
            yield return Dizer("maos");

            // Confirma o rastreamento: espera as mãos aparecerem (até 4 s).
            if (XRStartup.XRAtivo && HandPoints.Instancia != null)
            {
                for (var t = 0f; t < 4f && !HandPoints.Instancia.MaosVisiveis; t += Time.deltaTime) yield return null;
            }

            StartCoroutine(linhaDoTempo.AcenderBarra(2f));
            ctx.Enquadrar(linhaDoTempo.transform.position);
            yield return Dizer("seis");

            Mostrar(botaoComecar.gameObject, true);
            ctx.Enquadrar(botaoComecar.transform.position);
            ctx.Capturar("abertura");
            yield return EsperarToque(botaoComecar, "dica-comecar");
            Mostrar(botaoComecar.gameObject, false);

            yield return Dizer("siga");
        }
    }
}

using System.Collections;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Hall de entrada: o NPC narrador dá as boas-vindas, explica o objetivo
    /// em ~15 segundos e avisa que a movimentação é caminhando de verdade
    /// pela sala, dentro dos limites do Guardian. Depois abre a Feira.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class NarradorIntro : MonoBehaviour
    {
        [SerializeField] LinhaDeDialogo[] falas = new LinhaDeDialogo[0];
        [SerializeField] PainelLegenda painelLegenda;
        [SerializeField] AudioSource fonteDeVoz;
        [SerializeField] FadeVR fade;

        [SerializeField, Min(0f), Tooltip("Respiro antes de abrir a feira.")]
        float pausaFinal = 1.5f;

        void Awake()
        {
            if (fonteDeVoz == null) fonteDeVoz = GetComponent<AudioSource>();
            fonteDeVoz.playOnAwake = false;
            fonteDeVoz.spatialBlend = 1f;
            fonteDeVoz.dopplerLevel = 0f;
        }

        void Start() => StartCoroutine(Apresentar());

        IEnumerator Apresentar()
        {
            foreach (var fala in falas)
            {
                if (fala == null) continue;

                if (painelLegenda != null) painelLegenda.Mostrar(fala.legenda);

                if (fala.dublagem != null)
                {
                    fonteDeVoz.clip = fala.dublagem;
                    fonteDeVoz.Play();
                }

                yield return new WaitForSeconds(fala.Duracao);
            }

            if (painelLegenda != null) painelLegenda.Ocultar();
            yield return new WaitForSeconds(pausaFinal);

            if (fade != null)
            {
                var pronto = false;
                fade.Escurecer(() => pronto = true);
                while (!pronto) yield return null;
            }

            FluxoExperiencia.IrParaFeira();
        }

#if UNITY_EDITOR
        public void ConfigurarNoEditor(LinhaDeDialogo[] roteiro, PainelLegenda painel, FadeVR transicao)
        {
            falas = roteiro;
            painelLegenda = painel;
            fade = transicao;
        }
#endif
    }
}

using System.Collections;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Encerramento da jornada: partículas e luz quando o Mural do
    /// Conhecimento é completado, seguidos da tela de créditos.
    /// </summary>
    public class CelebracaoFinal : MonoBehaviour
    {
        [SerializeField] ParticleSystem[] particulas = new ParticleSystem[0];
        [SerializeField] Light luzDeCelebracao;
        [SerializeField] AudioSource fanfarra;
        [SerializeField] PainelLegenda painelLegenda;

        [SerializeField, TextArea(2, 4)]
        string mensagem = "Você percorreu toda a trilha da inovação maranhense!";

        [SerializeField, Min(0f), Tooltip("Tempo de celebração antes de ir para os créditos.")]
        float duracao = 6f;

        [SerializeField, Min(0f)] float intensidadeMaxima = 3f;

        bool jaDisparou;

        void Awake()
        {
            if (luzDeCelebracao != null) luzDeCelebracao.enabled = false;
        }

        public void Disparar()
        {
            if (jaDisparou) return;
            jaDisparou = true;
            StartCoroutine(Executar());
        }

        IEnumerator Executar()
        {
            foreach (var sistema in particulas)
                if (sistema != null) sistema.Play();

            if (fanfarra != null) fanfarra.Play();
            if (painelLegenda != null) painelLegenda.Mostrar(mensagem);

            if (luzDeCelebracao != null)
            {
                luzDeCelebracao.enabled = true;
                var subida = Mathf.Min(1f, duracao * 0.25f);
                for (var t = 0f; t < subida; t += Time.deltaTime)
                {
                    luzDeCelebracao.intensity = Mathf.Lerp(0f, intensidadeMaxima, t / subida);
                    yield return null;
                }
                luzDeCelebracao.intensity = intensidadeMaxima;
            }

            yield return new WaitForSeconds(duracao);

            if (painelLegenda != null) painelLegenda.Ocultar();

            FluxoExperiencia.IrParaCreditos();
        }

#if UNITY_EDITOR
        public void ConfigurarNoEditor(ParticleSystem[] sistemas, Light luz, PainelLegenda painel)
        {
            particulas = sistemas;
            luzDeCelebracao = luz;
            painelLegenda = painel;
        }
#endif
    }
}

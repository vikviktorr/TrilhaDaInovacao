using System.Collections;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Véu preso à câmera para as transições: clarão branco na entrada e na
    /// saída do túnel do tempo (<see cref="TunelDoTempo"/>) e fade preto (início
    /// e fim da visita). Em VR não há UI de tela cheia, então o véu é um quad no mundo.
    /// </summary>
    public class ScreenFader : MonoBehaviour
    {
        [SerializeField] Renderer veu;
        [SerializeField, Min(0.05f)] float duracaoFade = 0.5f;

        static readonly int CorBase = Shader.PropertyToID("_BaseColor");

        Material material;
        Color corAtual = Color.black;
        float opacidade = 1f;

        public float DuracaoFade => duracaoFade;

        void Awake()
        {
            if (veu != null) material = veu.material;
            Aplicar(Color.black, 1f);
        }

        public IEnumerator Fechar(Color cor, float? duracao = null)
        {
            yield return Animar(cor, opacidade, 1f, duracao ?? duracaoFade);
        }

        public IEnumerator Abrir(float? duracao = null)
        {
            yield return Animar(corAtual, opacidade, 0f, duracao ?? duracaoFade);
        }

        public void FecharImediato(Color cor) => Aplicar(cor, 1f);

        IEnumerator Animar(Color cor, float de, float para, float duracao)
        {
            for (var t = 0f; t < duracao; t += Time.deltaTime)
            {
                Aplicar(cor, Mathf.Lerp(de, para, Mathf.SmoothStep(0f, 1f, t / duracao)));
                yield return null;
            }

            Aplicar(cor, para);
        }

        void Aplicar(Color cor, float alfa)
        {
            corAtual = cor;
            opacidade = Mathf.Clamp01(alfa);
            if (material != null)
            {
                cor.a = opacidade;
                material.SetColor(CorBase, cor);
            }

            if (veu != null) veu.enabled = opacidade > 0.001f;
        }

#if UNITY_EDITOR
        public void ConfigurarNoEditor(Renderer quad)
        {
            veu = quad;
        }
#endif
    }
}

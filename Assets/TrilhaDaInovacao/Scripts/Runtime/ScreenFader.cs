using System.Collections;
using TMPro;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Véu preso à câmera para as transições: fade branco com o ano grande no
    /// centro (troca de época) e fade preto (início e fim da visita). Em VR
    /// não há UI de tela cheia, então o véu é um quad no mundo.
    /// </summary>
    public class ScreenFader : MonoBehaviour
    {
        [SerializeField] Renderer veu;
        [SerializeField] TMP_Text textoAno;
        [SerializeField, Min(0.05f)] float duracaoFade = 0.5f;
        [SerializeField, Min(0f)] float tempoDoAno = 1.5f;

        static readonly int CorBase = Shader.PropertyToID("_BaseColor");

        Material material;
        Color corAtual = Color.black;
        float opacidade = 1f;

        public float DuracaoFade => duracaoFade;
        public float TempoDoAno => tempoDoAno;

        void Awake()
        {
            if (veu != null) material = veu.material;
            Aplicar(Color.black, 1f);
            if (textoAno != null) textoAno.gameObject.SetActive(false);
        }

        public IEnumerator Fechar(Color cor, float? duracao = null)
        {
            yield return Animar(cor, opacidade, 1f, duracao ?? duracaoFade);
        }

        public IEnumerator Abrir(float? duracao = null)
        {
            if (textoAno != null) textoAno.gameObject.SetActive(false);
            yield return Animar(corAtual, opacidade, 0f, duracao ?? duracaoFade);
        }

        /// <summary>Mostra o ano sobre o véu já fechado.</summary>
        public void MostrarAno(string ano)
        {
            if (textoAno == null) return;
            textoAno.text = ano;
            textoAno.color = corAtual.grayscale > 0.5f ? new Color(0.08f, 0.12f, 0.22f) : Color.white;
            textoAno.gameObject.SetActive(!string.IsNullOrEmpty(ano));
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
        public void ConfigurarNoEditor(Renderer quad, TMP_Text ano)
        {
            veu = quad;
            textoAno = ano;
        }
#endif
    }
}

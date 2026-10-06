using System.Collections;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Escurece a visão com um quad preso à câmera. Em VR não se usa fade de
    /// UI em Screen Space Overlay, então o fade precisa existir no mundo.
    /// </summary>
    public class FadeVR : MonoBehaviour
    {
        [SerializeField] Renderer quad;
        [SerializeField, Min(0.05f)] float duracao = 0.6f;
        [SerializeField, Tooltip("Começa preto e abre ao entrar na cena.")]
        bool abrirAoIniciar = true;

        static readonly int CorBase = Shader.PropertyToID("_BaseColor");

        Material material;

        void Awake()
        {
            if (quad != null) material = quad.material;
            DefinirOpacidade(1f);
        }

        void Start()
        {
            if (abrirAoIniciar) StartCoroutine(Transicionar(1f, 0f));
        }

        public void Escurecer(System.Action aoTerminar = null)
            => StartCoroutine(Transicionar(0f, 1f, aoTerminar));

        public void Clarear(System.Action aoTerminar = null)
            => StartCoroutine(Transicionar(1f, 0f, aoTerminar));

        IEnumerator Transicionar(float de, float para, System.Action aoTerminar = null)
        {
            for (var t = 0f; t < duracao; t += Time.deltaTime)
            {
                DefinirOpacidade(Mathf.Lerp(de, para, t / duracao));
                yield return null;
            }

            DefinirOpacidade(para);
            aoTerminar?.Invoke();
        }

        void DefinirOpacidade(float alfa)
        {
            if (material == null) return;
            var cor = material.HasProperty(CorBase) ? material.GetColor(CorBase) : Color.black;
            cor.a = Mathf.Clamp01(alfa);
            if (material.HasProperty(CorBase)) material.SetColor(CorBase, cor);
            if (quad != null) quad.enabled = alfa > 0.001f;
        }

#if UNITY_EDITOR
        public void ConfigurarNoEditor(Renderer alvo) => quad = alvo;
#endif
    }
}

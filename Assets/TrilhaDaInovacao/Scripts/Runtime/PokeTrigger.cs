using System;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Alvo de toque com a mão. Segue as recomendações de rastreamento de mãos
    /// da Meta citadas no paper: alvo grande (no mínimo 15 cm), brilho que
    /// cresce conforme a mão se aproxima (até 20 cm) e som ao tocar. No modo
    /// desktop o <see cref="DesktopRig"/> chama <see cref="Tocar"/> pelo clique.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PokeTrigger : MonoBehaviour
    {
        [SerializeField] Renderer[] destaque;
        [SerializeField] Color corBrilho = new Color(1f, 0.85f, 0.35f);
        [SerializeField, Min(0.01f)] float distanciaBrilho = 0.2f;
        [SerializeField, Min(0.001f)] float distanciaToque = 0.025f;
        [SerializeField, Min(0f)] float intervalo = 0.6f;

        static readonly int Emissao = Shader.PropertyToID("_EmissionColor");

        Collider area;
        Material[] materiais;
        Color[] emissaoOriginal;
        AudioSource fonte;
        float ultimoToque = -10f;
        bool aguardando;

        /// <summary>Disparado no toque válido (só enquanto ativo).</summary>
        public event Action Tocado;

        public bool Ativo { get; private set; }

        void Awake()
        {
            area = GetComponent<Collider>();
            area.isTrigger = true;
            fonte = gameObject.AddComponent<AudioSource>();
            fonte.playOnAwake = false;
            fonte.spatialBlend = 1f;

            if (destaque == null || destaque.Length == 0) destaque = GetComponentsInChildren<Renderer>();
            materiais = new Material[destaque.Length];
            emissaoOriginal = new Color[destaque.Length];
            for (var i = 0; i < destaque.Length; i++)
            {
                materiais[i] = destaque[i].material;
                materiais[i].EnableKeyword("_EMISSION");
                emissaoOriginal[i] = materiais[i].HasProperty(Emissao) ? materiais[i].GetColor(Emissao) : Color.black;
            }
        }

        /// <summary>Liga o alvo: passa a pulsar e aceitar toque.</summary>
        public void Ativar()
        {
            Ativo = true;
            aguardando = true;
        }

        public void Desativar()
        {
            Ativo = false;
            aguardando = false;
            Brilhar(0f);
        }

        void Update()
        {
            if (!Ativo) return;

            var maisPerto = float.MaxValue;
            var pontas = HandPoints.Instancia != null ? HandPoints.Instancia.Pontas : null;
            if (pontas != null)
            {
                foreach (var ponta in pontas)
                {
                    var d = Vector3.Distance(area.ClosestPoint(ponta), ponta);
                    maisPerto = Mathf.Min(maisPerto, d);
                }
            }

            var aproximacao = 1f - Mathf.Clamp01(maisPerto / distanciaBrilho);
            var chamada = aguardando ? 0.35f + 0.25f * Mathf.Sin(Time.time * 5f) : 0f;
            Brilhar(Mathf.Max(aproximacao * 1.6f, chamada));

            if (maisPerto <= distanciaToque) Tocar();
        }

        /// <summary>Toque confirmado (mão, controle ou clique no modo desktop).</summary>
        public void Tocar()
        {
            if (!Ativo || Time.time - ultimoToque < intervalo) return;
            ultimoToque = Time.time;
            aguardando = false;
            fonte.PlayOneShot(ProceduralAudio.Obter("chime"));
            Tocado?.Invoke();
        }

        void Brilhar(float intensidade)
        {
            if (materiais == null) return;
            for (var i = 0; i < materiais.Length; i++)
            {
                materiais[i].SetColor(Emissao, emissaoOriginal[i] + corBrilho * intensidade);
            }
        }

#if UNITY_EDITOR
        public void ConfigurarNoEditor(Renderer[] renderers, Color cor)
        {
            destaque = renderers;
            corBrilho = cor;
        }
#endif
    }
}

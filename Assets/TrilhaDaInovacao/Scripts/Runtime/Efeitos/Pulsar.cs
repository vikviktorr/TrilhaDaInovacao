using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Chama atenção para um objeto: pulsa o brilho (emissão) e, opcionalmente,
    /// a escala. Usado no telefone tocando, no ponto do mapa, na lâmpada etc.
    /// </summary>
    public class Pulsar : MonoBehaviour
    {
        public Renderer alvo;
        public Color cor = Color.white;
        public float frequencia = 2f;
        public float intensidade = 1.5f;
        public float escala = 0f;
        public bool ativo = true;

        static readonly int Emissao = Shader.PropertyToID("_EmissionColor");

        Material material;
        Color emissaoBase;
        Vector3 escalaBase;
        PokeTrigger toque;

        void Awake()
        {
            if (alvo == null) alvo = GetComponent<Renderer>();
            if (alvo != null)
            {
                material = alvo.material;
                material.EnableKeyword("_EMISSION");
                emissaoBase = material.GetColor(Emissao);
            }

            escalaBase = transform.localScale;
            toque = GetComponentInParent<PokeTrigger>();
        }

        void Update()
        {
            // Enquanto o alvo espera o toque, o brilho é do PokeTrigger.
            if (toque != null && toque.Ativo) return;
            var k = ativo ? 0.5f + 0.5f * Mathf.Sin(Time.time * frequencia * 2f * Mathf.PI) : 0f;
            if (material != null) material.SetColor(Emissao, emissaoBase + cor * (k * intensidade));
            if (escala > 0f) transform.localScale = escalaBase * (1f + escala * k);
        }

        public void Definir(bool ligado)
        {
            ativo = ligado;
            if (!ligado) transform.localScale = escalaBase;
        }
    }
}

using TMPro;
using UnityEngine;

namespace TrilhaDaInovacao
{
    public enum EstadoMarca { Apagada, Proxima, Visitada }

    /// <summary>
    /// Marca no piso, espelho virtual da fita colada no chão do Sebraelab.
    /// Pulsa quando é a próxima parada e detecta quando a cabeça do visitante
    /// está dentro do raio (só no plano do chão, a altura não importa).
    /// </summary>
    public class FloorMarker : MonoBehaviour
    {
        [SerializeField] int indice;
        [SerializeField] LineRenderer anel;
        [SerializeField] Renderer disco;
        [SerializeField] TMP_Text rotulo;
        [SerializeField] float raio = LayoutTrilha.RaioMarca;

        static readonly int CorBase = Shader.PropertyToID("_BaseColor");

        static readonly Color CorProxima = new Color(0.35f, 0.85f, 1f);
        static readonly Color CorVisitada = new Color(0.35f, 0.42f, 0.5f);

        Material materialDisco;
        EstadoMarca estado;

        public int Indice => indice;
        public EstadoMarca Estado => estado;

        void Awake()
        {
            if (disco != null) materialDisco = disco.material;
            Definir(EstadoMarca.Apagada);
        }

        public bool ContemCabeca(Vector3 cabeca)
        {
            var d = LayoutTrilha.PlanoXZ(cabeca) - LayoutTrilha.PlanoXZ(transform.position);
            return d.sqrMagnitude <= raio * raio;
        }

        public void Definir(EstadoMarca novo)
        {
            estado = novo;
            var visivel = novo != EstadoMarca.Apagada;
            if (anel != null) anel.enabled = visivel;
            if (disco != null) disco.enabled = novo == EstadoMarca.Proxima;
            if (rotulo != null) rotulo.gameObject.SetActive(visivel);
            Pintar(novo == EstadoMarca.Proxima ? CorProxima : CorVisitada, 1f);
        }

        void Update()
        {
            if (estado != EstadoMarca.Proxima) return;
            var pulso = 0.55f + 0.45f * Mathf.Sin(Time.time * 4f);
            Pintar(CorProxima, pulso);
        }

        void Pintar(Color cor, float intensidade)
        {
            if (anel != null)
            {
                var c = cor * (0.6f + 0.8f * intensidade);
                c.a = 1f;
                anel.startColor = anel.endColor = c;
                anel.widthMultiplier = estado == EstadoMarca.Proxima ? 0.05f + 0.02f * intensidade : 0.03f;
            }

            if (materialDisco != null)
            {
                var c = cor;
                c.a = 0.12f + 0.18f * intensidade;
                materialDisco.SetColor(CorBase, c);
            }

            if (rotulo != null) rotulo.color = estado == EstadoMarca.Proxima ? Color.white : cor;
        }

#if UNITY_EDITOR
        public void ConfigurarNoEditor(int i, LineRenderer linha, Renderer fundo, TMP_Text texto)
        {
            indice = i;
            anel = linha;
            disco = fundo;
            rotulo = texto;
        }
#endif
    }
}

using TMPro;
using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Um setor (década) do Mural do Conhecimento. Começa apagado e acende
    /// quando o usuário termina de ouvir o NPC daquele estande.
    /// </summary>
    public class SetorMural : MonoBehaviour
    {
        [SerializeField] EstandeData estande;
        [SerializeField] Renderer placa;
        [SerializeField] TMP_Text rotulo;
        [SerializeField] Light luz;

        [Header("Cores por estado")]
        [SerializeField] Color corBloqueado = new Color(0.16f, 0.17f, 0.20f);
        [SerializeField] Color corVisitado = new Color(0.55f, 0.45f, 0.12f);
        [SerializeField] Color corConcluido = new Color(0.10f, 0.72f, 0.55f);
        [SerializeField, Min(0f)] float intensidadeEmissiva = 2.2f;

        static readonly int CorBase = Shader.PropertyToID("_BaseColor");
        static readonly int CorEmissiva = Shader.PropertyToID("_EmissionColor");

        MaterialPropertyBlock bloco;

        public EstandeData Estande => estande;

        void Awake()
        {
            bloco = new MaterialPropertyBlock();
            DefinirEstado(EstadoEstande.Bloqueado);
        }

        public void DefinirEstado(EstadoEstande estado)
        {
            var cor = estado switch
            {
                EstadoEstande.Concluido => corConcluido,
                EstadoEstande.Visitado => corVisitado,
                _ => corBloqueado
            };

            if (placa != null)
            {
                bloco ??= new MaterialPropertyBlock();
                placa.GetPropertyBlock(bloco);
                bloco.SetColor(CorBase, cor);
                var emissiva = estado == EstadoEstande.Concluido ? cor * intensidadeEmissiva : Color.black;
                bloco.SetColor(CorEmissiva, emissiva);
                placa.SetPropertyBlock(bloco);
            }

            if (luz != null)
            {
                luz.color = cor;
                luz.enabled = estado == EstadoEstande.Concluido;
            }

            if (rotulo != null && estande != null)
                rotulo.text = estande.RotuloCurto;
        }

#if UNITY_EDITOR
        public void ConfigurarNoEditor(EstandeData dados, Renderer alvo, TMP_Text texto, Light luzSetor)
        {
            estande = dados;
            placa = alvo;
            rotulo = texto;
            luz = luzSetor;
        }
#endif
    }
}

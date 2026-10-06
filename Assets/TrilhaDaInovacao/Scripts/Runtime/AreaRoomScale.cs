using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Representa, em escala 1:1, a sala física do Sebrae cedida à equipe.
    /// A locomoção é o caminhar real do usuário — não há teleporte — então
    /// todo estande precisa caber dentro deste retângulo e o Guardian do
    /// Quest 2 deve ser configurado com o mesmo perímetro.
    /// </summary>
    [ExecuteAlways]
    public class AreaRoomScale : MonoBehaviour
    {
        [Header("Medidas reais da sala (metros)")]
        [SerializeField, Min(1f), Tooltip("PROVISÓRIO: medir in loco antes de fechar o layout.")]
        float largura = 6f;
        [SerializeField, Min(1f)]
        float profundidade = 8f;

        [Header("Folga de segurança")]
        [SerializeField, Min(0f), Tooltip("Faixa junto à parede que deve ficar livre de estandes.")]
        float margem = 0.6f;

        public float Largura => largura;
        public float Profundidade => profundidade;
        public float Margem => margem;

        public Vector2 TamanhoUtil => new Vector2(
            Mathf.Max(0f, largura - margem * 2f),
            Mathf.Max(0f, profundidade - margem * 2f));

        /// <summary>Um ponto está dentro da área caminhável real?</summary>
        public bool DentroDaArea(Vector3 posicaoMundo)
        {
            var local = transform.InverseTransformPoint(posicaoMundo);
            var util = TamanhoUtil;
            return Mathf.Abs(local.x) <= util.x * 0.5f && Mathf.Abs(local.z) <= util.y * 0.5f;
        }

        void OnDrawGizmos()
        {
            var matriz = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            // Perímetro real da sala.
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.9f);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(largura, 0.02f, profundidade));

            // Área útil, onde os estandes podem ficar.
            var util = TamanhoUtil;
            Gizmos.color = new Color(0.2f, 0.9f, 0.5f, 0.6f);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(util.x, 0.02f, util.y));

            Gizmos.matrix = matriz;
        }

#if UNITY_EDITOR
        public void ConfigurarNoEditor(float larguraSala, float profundidadeSala)
        {
            largura = larguraSala;
            profundidade = profundidadeSala;
        }
#endif
    }
}

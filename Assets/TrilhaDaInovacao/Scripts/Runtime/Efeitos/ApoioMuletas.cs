using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Personagem em pé apoiado em duas muletas canadenses (as do próprio modelo
    /// Kenney, presas ao antebraço). Depois da animação, cada braço é apontado
    /// para que a ponteira da muleta fique parada no chão um pouco à frente e
    /// ao lado do pé, a "posição de tripé" ensinada na fisioterapia: o peso se
    /// divide entre as pernas e as duas muletas. O tronco inclina um pouco para
    /// a frente, sobre as muletas, e o peso passa devagar de um lado para o outro.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class ApoioMuletas : MonoBehaviour
    {
        [SerializeField] Transform bracoEsquerdo;
        [SerializeField] Transform bracoDireito;
        [SerializeField] Transform tronco;
        [SerializeField, Tooltip("Ponteiras no espaço local do osso de cada braço (esq., dir.).")]
        Vector3 ponteiraEsquerda, ponteiraDireita;
        [SerializeField, Tooltip("Altura do chão no mundo, onde as ponteiras se apoiam.")]
        float chao;

        [Tooltip("Inclinação do tronco para a frente, sobre as muletas (graus).")]
        public float inclinacao = 9f;
        [Tooltip("Quanto da distância horizontal até a ponteira vai para a frente (0) ou para o lado (1).")]
        [Range(0f, 1f)] public float abertura = 0.55f;
        [Tooltip("Graus que o tronco balança ao passar o peso de um lado para o outro.")]
        public float balanco = 2.5f;
        public float periodo = 5.5f;

        Vector3 apoioEsquerdo, apoioDireito;
        bool pronto;
        float fase;

        void Start()
        {
            fase = Random.Range(0f, 10f);
        }

        void LateUpdate()
        {
            if (bracoEsquerdo == null || bracoDireito == null) return;

            // Frente e direita do corpo, tiradas dos ombros (o modelo pode vir girado).
            var direita = bracoDireito.position - bracoEsquerdo.position;
            direita.y = 0f;
            direita.Normalize();
            var frente = Vector3.Cross(direita, Vector3.up);

            var t = (Time.time + fase) * Mathf.PI * 2f / periodo;
            if (tronco != null)
            {
                // Inclina sobre as muletas e passa o peso de um lado para o outro.
                tronco.rotation = Quaternion.AngleAxis(inclinacao + 1.2f * Mathf.Sin(t * 2f), direita)
                                  * Quaternion.AngleAxis(balanco * Mathf.Sin(t), frente) * tronco.rotation;
            }

            // Os pontos de apoio são escolhidos uma vez, com o tronco já inclinado:
            // a ponteira toca o chão exatamente no comprimento do braço + muleta.
            if (!pronto)
            {
                apoioEsquerdo = PontoDeApoio(bracoEsquerdo, ponteiraEsquerda, frente, -direita);
                apoioDireito = PontoDeApoio(bracoDireito, ponteiraDireita, frente, direita);
                pronto = true;
            }

            Apontar(bracoEsquerdo, ponteiraEsquerda, apoioEsquerdo);
            Apontar(bracoDireito, ponteiraDireita, apoioDireito);
        }

        Vector3 PontoDeApoio(Transform braco, Vector3 ponteira, Vector3 frente, Vector3 fora)
        {
            var ombro = braco.position;
            var alcance = braco.TransformVector(ponteira).magnitude;
            var altura = Mathf.Max(0.01f, ombro.y - chao);
            var horizontal = Mathf.Sqrt(Mathf.Max(0f, alcance * alcance - altura * altura));
            var direcao = (frente * (1f - abertura) + fora * abertura).normalized;
            return new Vector3(ombro.x, chao, ombro.z) + direcao * horizontal;
        }

        static void Apontar(Transform braco, Vector3 ponteira, Vector3 apoio)
        {
            var atual = braco.TransformVector(ponteira);
            var desejado = apoio - braco.position;
            braco.rotation = Quaternion.FromToRotation(atual, desejado) * braco.rotation;
        }

#if UNITY_EDITOR
        public void ConfigurarNoEditor(Transform esquerdo, Transform direito, Transform corpo, Vector3 pontaEsq, Vector3 pontaDir, float alturaChao)
        {
            chao = alturaChao;
            bracoEsquerdo = esquerdo;
            bracoDireito = direito;
            tronco = corpo;
            ponteiraEsquerda = pontaEsq;
            ponteiraDireita = pontaDir;
        }
#endif
    }
}

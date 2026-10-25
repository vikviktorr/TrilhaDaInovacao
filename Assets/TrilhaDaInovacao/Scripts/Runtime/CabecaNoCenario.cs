using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// No VR o visitante anda no espaço real e pode enfiar a cabeça num móvel
    /// virtual (ou nascer dentro de um quando a época troca). Mover o corpo
    /// descalibraria a sala, então, como é praxe em VR, a visão escurece enquanto
    /// a cabeça está dentro do cenário (camada <see cref="LayoutTrilha.CamadaCenario"/>)
    /// e clareia ao sair. Fica na câmera; o <see cref="VisitDirector"/> o liga.
    /// </summary>
    public class CabecaNoCenario : MonoBehaviour
    {
        [SerializeField, Tooltip("Raio da cabeça, em metros.")] float raio = 0.12f;
        [SerializeField, Tooltip("Segundos para escurecer e para clarear.")] float rampa = 0.25f;
        [SerializeField, Range(0f, 1f)] float maximo = 0.92f;

        const int Mascara = 1 << LayoutTrilha.CamadaCenario;
        static readonly Vector3[] Eixos = { Vector3.up, Vector3.right, Vector3.forward };

        ScreenFader fader;
        float nivel;

        public void Configurar(ScreenFader alvo) => fader = alvo;

        void LateUpdate()
        {
            if (fader == null) return;
            var dentro = Dentro(transform.position);
            nivel = Mathf.MoveTowards(nivel, dentro ? maximo : 0f, Time.unscaledDeltaTime / rampa);
            fader.Obstrucao = nivel;
        }

        /// <summary>
        /// Encostando numa superfície, ou envolvida por um volume fechado: um eixo
        /// em que os dois sentidos batem primeiro no lado de dentro (face de trás).
        /// Paredes da sala vistas de dentro dela mostram a face da frente e não contam.
        /// </summary>
        bool Dentro(Vector3 cabeca)
        {
            if (Physics.CheckSphere(cabeca, raio, Mascara, QueryTriggerInteraction.Ignore)) return true;

            var antes = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;
            var envolvida = false;
            foreach (var eixo in Eixos)
            {
                if (DeDentro(cabeca, eixo) && DeDentro(cabeca, -eixo))
                {
                    envolvida = true;
                    break;
                }
            }

            Physics.queriesHitBackfaces = antes;
            return envolvida;
        }

        static bool DeDentro(Vector3 origem, Vector3 direcao)
            => Physics.Raycast(origem, direcao, out var acerto, 1.5f, Mascara, QueryTriggerInteraction.Ignore)
               && Vector3.Dot(acerto.normal, direcao) > 0f;
    }
}

using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Abaixa um som de efeito ou ambiente enquanto alguém fala (ducking), para
    /// a voz do guia e dos figurantes ficar sempre acima. Atua no sinal do
    /// AudioSource deste objeto, então não briga com o volume que as cenas definem.
    /// O <see cref="VisitDirector"/> liga e desliga pelo <see cref="Alvo"/>.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class Abafador : MonoBehaviour
    {
        /// <summary>Ganho desejado para todos os efeitos: 1 = normal.</summary>
        public static volatile float Alvo = 1f;

        // Rampa de ~0,15 s a 48 kHz: sem estalo ao abaixar ou voltar.
        const float Passo = 1f / 7000f;
        float ganho = 1f;

        void OnAudioFilterRead(float[] dados, int canais)
        {
            var alvo = Alvo;
            for (var i = 0; i < dados.Length; i += canais)
            {
                ganho += Mathf.Clamp(alvo - ganho, -Passo, Passo);
                for (var c = 0; c < canais; c++) dados[i + c] *= ganho;
            }
        }
    }
}

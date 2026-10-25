using UnityEngine;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Aplica o volume das vozes escolhido pelo visitante (<see cref="ConfigAudio.Vozes"/>)
    /// no sinal do AudioSource deste objeto, como o <see cref="Abafador"/> faz com os
    /// efeitos. O <see cref="VisitDirector"/> o põe na voz do guia e dos figurantes.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class GanhoVoz : MonoBehaviour
    {
        // Rampa de ~0,15 s a 48 kHz: sem estalo ao mudar o volume.
        const float Passo = 1f / 7000f;
        float ganho = -1f;

        void OnAudioFilterRead(float[] dados, int canais)
        {
            var alvo = ConfigAudio.Vozes;
            if (ganho < 0f) ganho = alvo;
            for (var i = 0; i < dados.Length; i += canais)
            {
                ganho += Mathf.Clamp(alvo - ganho, -Passo, Passo);
                for (var c = 0; c < canais; c++) dados[i + c] *= ganho;
            }
        }
    }
}

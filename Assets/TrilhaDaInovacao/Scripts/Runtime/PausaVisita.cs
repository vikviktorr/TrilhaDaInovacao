using System;
using UnityEngine;
using UnityEngine.XR;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Pausa a visita quando o visitante tira o óculos ou abre o menu do sistema
    /// (VRC.Quest.Functional.2 e VRC.Quest.Input.4 da Meta): o tempo do jogo e
    /// o áudio param, e as mãos somem (<see cref="HandPoints"/>). Ao voltar, a
    /// visita continua de onde estava; se o óculos ficou fora mais que
    /// <see cref="limiteAusencia"/>, quem voltou é outra pessoa e a visita
    /// recomeça do início.
    /// Só vale com XR ativo: no PC, trocar de janela não pausa. O modo -qa usa
    /// <see cref="Simular"/> para testar a pausa sem óculos.
    /// </summary>
    public class PausaVisita : MonoBehaviour
    {
        [Tooltip("Segundos sem ninguém no óculos até a visita recomeçar do início.")]
        public float limiteAusencia = 45f;

        public static bool Pausada { get; private set; }

        /// <summary>Retomou depois de uma ausência: true se recomeçou a visita.</summary>
        public event Action<float, bool> Retomou;

        VisitDirector diretor;
        bool foraDoFoco, appPausado, semUsuario, simulada;
        DateTime inicioAusencia;

        void Awake()
        {
            diretor = GetComponent<VisitDirector>();
            Pausada = false;
        }

        void OnDestroy()
        {
            if (Pausada) Liberar();
        }

        // Menu universal aberto: o app perde o foco, mas continua desenhando.
        void OnApplicationFocus(bool foco)
        {
            foraDoFoco = !foco;
            Avaliar();
        }

        // Óculos tirado (sensor de proximidade) ou em repouso: o app é pausado e o
        // Update para de rodar; por isso a ausência é medida no relógio do sistema.
        void OnApplicationPause(bool pausa)
        {
            appPausado = pausa;
            Avaliar();
        }

        void Update()
        {
            if (XRStartup.XRAtivo)
            {
                // Nem todo runtime informa a presença; sem ela, valem o foco e a pausa do app.
                var cabeca = InputDevices.GetDeviceAtXRNode(XRNode.Head);
                semUsuario = cabeca.isValid && cabeca.TryGetFeatureValue(CommonUsages.userPresence, out var presente) && !presente;
            }

            Avaliar();
        }

        /// <summary>QA: finge que o visitante tirou (true) ou recolocou (false) o óculos.</summary>
        public void Simular(bool ausente)
        {
            simulada = ausente;
            Avaliar();
        }

        void Avaliar()
        {
            var ausente = simulada || (XRStartup.XRAtivo && (foraDoFoco || appPausado || semUsuario));
            if (ausente == Pausada) return;
            if (ausente) Pausar();
            else Retomar();
        }

        void Pausar()
        {
            Pausada = true;
            inicioAusencia = DateTime.UtcNow;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            diretor.Registro.Gatilho("pausa");
            Debug.Log("[Trilha da Inovação] Visitante saiu do óculos: visita pausada.");
        }

        void Retomar()
        {
            var fora = (float)(DateTime.UtcNow - inicioAusencia).TotalSeconds;
            Liberar();
            var recomecar = fora >= limiteAusencia;
            Debug.Log(recomecar
                ? $"[Trilha da Inovação] Óculos ficou {fora:0} s sem ninguém: visita recomeça para o próximo visitante."
                : $"[Trilha da Inovação] Visitante voltou depois de {fora:0.0} s: visita continua.");
            if (recomecar) diretor.Reiniciar();
            Retomou?.Invoke(fora, recomecar);
        }

        static void Liberar()
        {
            Pausada = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }
}

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Management;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Decide entre VR e modo desktop. No Quest o OpenXR já inicia sozinho.
    /// No PC a inicialização é manual: se não houver runtime OpenXR (ou com
    /// o argumento -desktop), a visita roda com teclado e mouse.
    /// </summary>
    public static class XRStartup
    {
        public static bool XRAtivo { get; private set; }
        public static bool Pronto { get; private set; }

        static string[] argumentos;

        /// <summary>
        /// Argumentos da linha de comando. No Quest vêm do extra "args" do intent:
        /// adb shell am start -n &lt;pacote&gt;/com.unity3d.player.UnityPlayerActivity -e args "-qa -visitas 3"
        /// </summary>
        static string[] Argumentos => argumentos ??= LerArgumentos();

        static string[] LerArgumentos()
        {
            var lista = new System.Collections.Generic.List<string>(Environment.GetCommandLineArgs());
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var atividade = unity.GetStatic<AndroidJavaObject>("currentActivity");
                using var intent = atividade.Call<AndroidJavaObject>("getIntent");
                var extra = intent.Call<string>("getStringExtra", "args");
                if (!string.IsNullOrEmpty(extra)) lista.AddRange(extra.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Trilha da Inovação] Não deu para ler os argumentos do intent: {e.Message}");
            }
#endif
            return lista.ToArray();
        }

        public static bool TemArgumento(string nome)
            => Array.Exists(Argumentos, a => string.Equals(a, nome, StringComparison.OrdinalIgnoreCase));

        public static string ValorDoArgumento(string nome)
        {
            var args = Argumentos;
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], nome, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            }

            return null;
        }

        public static IEnumerator Inicializar()
        {
            var gerente = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;

            if (gerente == null)
            {
                Concluir(false, "sem configuração de XR");
                yield break;
            }

            if (gerente.isInitializationComplete && gerente.activeLoader != null)
            {
                Concluir(true, "iniciado pelo sistema");
                yield break;
            }

            if (TemArgumento("-desktop") || TemArgumento("-tour") || TemArgumento("-qa"))
            {
                Concluir(false, "argumento de linha de comando");
                yield break;
            }

            yield return gerente.InitializeLoader();

            if (gerente.activeLoader == null)
            {
                Concluir(false, "nenhum runtime OpenXR encontrado");
                yield break;
            }

            gerente.StartSubsystems();
            Concluir(true, gerente.activeLoader.name);
        }

        public static void Encerrar()
        {
            var gerente = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
            if (gerente == null || !gerente.isInitializationComplete) return;
            gerente.StopSubsystems();
            gerente.DeinitializeLoader();
        }

        static void Concluir(bool xr, string motivo)
        {
            XRAtivo = xr;
            Pronto = true;
            Debug.Log($"[Trilha da Inovação] Modo {(xr ? "VR" : "desktop")} ({motivo}).");
        }
    }
}

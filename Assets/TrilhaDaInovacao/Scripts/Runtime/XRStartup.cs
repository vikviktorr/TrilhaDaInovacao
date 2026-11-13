using System;
using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Management;

namespace TrilhaDaInovacao
{
    /// <summary>
    /// Decide entre VR e modo desktop. No Quest o OpenXR já inicia sozinho.
    /// No PC a inicialização é manual: se não houver runtime OpenXR (ou com
    /// o argumento -desktop), a visita roda com teclado e mouse.
    /// No editor, com o simulador ligado, o XR Interaction Simulator faz o papel do óculos.
    /// </summary>
    public static class XRStartup
    {
        public static bool XRAtivo { get; private set; }
        public static bool Pronto { get; private set; }

        /// <summary>VR simulado no editor: teclado e mouse movem a cabeça, os controles e as mãos.</summary>
        public static bool Simulado { get; private set; }

        public const string ChaveSimulador = "TrilhaDaInovacao.SimularVR";
        const string PrefabSimulador = "Assets/Samples/XR Interaction Toolkit/3.6.1/XR Interaction Simulator/XR Interaction Simulator.prefab";
        const float AlturaOlhosSimulada = 1.6f;

#if UNITY_EDITOR
        /// <summary>Ligado por padrão no Linux, onde o OpenXR não roda; nos outros, só pelo menu.</summary>
        public static bool SimuladorLigado
        {
            get => UnityEditor.EditorPrefs.GetBool(ChaveSimulador, Application.platform == RuntimePlatform.LinuxEditor);
            set => UnityEditor.EditorPrefs.SetBool(ChaveSimulador, value);
        }
#endif

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
#if UNITY_EDITOR
            if (SimuladorLigado && IniciarSimulador())
            {
                Simulado = true;
                Concluir(true, "XR Interaction Simulator");
                yield break;
            }
#endif

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

            // O pacote OpenXR não traz a biblioteca nativa para Linux: tentar
            // inicializar lança DllNotFoundException e derruba a corrotina.
            if (Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer)
            {
                Concluir(false, "OpenXR não suportado no Linux");
                yield break;
            }

            // O InitializeLoader não captura exceções: se a biblioteca nativa do
            // OpenXR falhar ao carregar (DllNotFoundException no Windows, por
            // antivírus, CPU ARM ou runtime do Visual C++ ausente), a exceção
            // derrubava a corrotina e a tela ficava preta. Cai no modo desktop.
            var passos = gerente.InitializeLoader();
            while (true)
            {
                object passo;
                try
                {
                    if (!passos.MoveNext()) break;
                    passo = passos.Current;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Trilha da Inovação] O OpenXR falhou ao iniciar: {e.GetType().Name}: {e.Message}");
                    Concluir(false, "falha ao carregar o OpenXR");
                    yield break;
                }

                yield return passo;
            }

            if (gerente.activeLoader == null)
            {
                Concluir(false, "nenhum runtime OpenXR encontrado");
                yield break;
            }

            gerente.StartSubsystems();
            Concluir(true, gerente.activeLoader.name);
        }

#if UNITY_EDITOR
        static bool IniciarSimulador()
        {
            if (XRInteractionSimulator.instance == null)
            {
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabSimulador);
                if (prefab == null)
                {
                    Debug.LogWarning($"[Trilha da Inovação] Simulador de VR não encontrado em {PrefabSimulador}.");
                    return false;
                }

                var simulador = UnityEngine.Object.Instantiate(prefab);
                simulador.name = prefab.name;
                UnityEngine.Object.DontDestroyOnLoad(simulador);
            }

            // O óculos simulado começa com a cabeça na origem do rastreamento, que no
            // room-scale é o chão: o Camera Offset sobe os olhos para a altura de um adulto.
            var origem = UnityEngine.Object.FindAnyObjectByType<XROrigin>();
            if (origem != null && origem.CameraFloorOffsetObject != null)
                origem.CameraFloorOffsetObject.transform.localPosition = Vector3.up * AlturaOlhosSimulada;

            return true;
        }
#endif

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

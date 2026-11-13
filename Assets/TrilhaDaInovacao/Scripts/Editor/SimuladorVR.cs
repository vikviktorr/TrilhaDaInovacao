using UnityEditor;
using UnityEngine;

namespace TrilhaDaInovacao.EditorTools
{
    /// <summary>
    /// Liga ou desliga o VR simulado no Play do editor (XR Interaction Simulator).
    /// Desligue para usar o óculos de verdade pelo Link, no Windows.
    /// </summary>
    static class SimuladorVR
    {
        const string Menu = "Trilha da Inovação/Simular VR no editor";

        [MenuItem(Menu)]
        static void Alternar()
        {
            XRStartup.SimuladorLigado = !XRStartup.SimuladorLigado;
            Debug.Log($"[Trilha da Inovação] VR simulado no editor {(XRStartup.SimuladorLigado ? "ligado" : "desligado")}.");
        }

        [MenuItem(Menu, true)]
        static bool Validar()
        {
            UnityEditor.Menu.SetChecked(Menu, XRStartup.SimuladorLigado);
            return !EditorApplication.isPlaying;
        }
    }
}

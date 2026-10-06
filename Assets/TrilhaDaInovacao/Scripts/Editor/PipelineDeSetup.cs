using UnityEditor;
using UnityEngine;

namespace TrilhaDaInovacao.EditorTools
{
    /// <summary>
    /// Ponto de entrada único: configura o XR e reconstrói o whitebox.
    /// Também é o alvo do -executeMethod em builds de linha de comando.
    /// </summary>
    public static class PipelineDeSetup
    {
        [MenuItem("Trilha da Inovação/0 · Configurar tudo (XR + cenas)")]
        public static void Tudo()
        {
            ConfiguradorXR.Configurar();
            ConstrutorDeCenas.ConstruirTudo();
            AssetDatabase.SaveAssets();
            Debug.Log("[Trilha da Inovação] Setup completo.");
        }
    }
}

using UnityEditor;
using UnityEditor.SceneManagement;

namespace TrilhaDaInovacao.EditorTools
{
    /// <summary>
    /// O Play do editor sempre começa pelo 00_Bootstrap, mesmo com o hub ou uma
    /// época aberta. As cenas são aditivas: abertas sozinhas, ficam sem rig,
    /// câmera nem audio listener.
    /// </summary>
    [InitializeOnLoad]
    static class PlayPeloBootstrap
    {
        const string Caminho = "Assets/Scenes/00_Bootstrap.unity";

        static PlayPeloBootstrap()
        {
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(Caminho);
        }
    }
}

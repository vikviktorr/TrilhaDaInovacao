using UnityEditor;

namespace TrilhaDaInovacao.EditorTools
{
    /// <summary>
    /// Configuração de importação dos modelos de terceiros (Kenney etc.):
    /// sem animação, câmeras, luzes ou colisores — no cenário eles são só
    /// visuais. Materiais vêm da descrição do FBX (URP converte para Lit).
    /// </summary>
    public class ImportadorModelos : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith("Assets/TrilhaDaInovacao/Modelos/")) return;

            var importador = (ModelImporter)assetImporter;
            importador.animationType = ModelImporterAnimationType.None;
            importador.importAnimation = false;
            importador.importCameras = false;
            importador.importLights = false;
            importador.importBlendShapes = false;
            importador.addCollider = false;
            importador.isReadable = false;
            importador.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        }
    }
}

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
        // Mudou a regra de importação? Suba a versão para o Unity reimportar.
        public override uint GetVersion() => 3;

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith("Assets/TrilhaDaInovacao/Modelos/")) return;

            var importador = (ModelImporter)assetImporter;
            // Personagens trazem animações (idle, interact...): Legacy para tocar sem Animator.
            var personagem = assetPath.Contains("/mini-characters/character-");
            importador.animationType = personagem ? ModelImporterAnimationType.Legacy : ModelImporterAnimationType.None;
            importador.importAnimation = personagem;
            importador.importCameras = false;
            importador.importLights = false;
            importador.importBlendShapes = false;
            importador.addCollider = false;
            // Legível só no editor: o construtor mescla as malhas; os originais não vão para o build.
            importador.isReadable = true;
            importador.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        }
    }
}

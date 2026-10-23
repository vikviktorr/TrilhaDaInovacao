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
        public override uint GetVersion() => 4;

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

        /// <summary>
        /// Paletas dos modelos feitos no Blender (Ferramentas/blender): cada cor é um
        /// quadradinho de poucos pixels, então nada de mipmap, filtro ou compressão,
        /// que misturariam as cores vizinhas.
        /// </summary>
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/TrilhaDaInovacao/Modelos/") || !assetPath.EndsWith("-paleta.png")) return;

            var importador = (TextureImporter)assetImporter;
            importador.mipmapEnabled = false;
            importador.filterMode = UnityEngine.FilterMode.Point;
            importador.textureCompression = TextureImporterCompression.Uncompressed;
            importador.wrapMode = UnityEngine.TextureWrapMode.Clamp;
        }
    }
}

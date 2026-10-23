using UnityEditor;
using UnityEngine;

namespace TrilhaDaInovacao.EditorTools
{
    /// <summary>
    /// Importação das vozes geradas (Ferramentas/voz/gerar_vozes.py): mono,
    /// Vorbis comprimido na memória e carregado em segundo plano, para os
    /// ~4 min de falas pesarem pouco no Quest 2.
    /// </summary>
    public class ImportadorVozes : AssetPostprocessor
    {
        // Mudou a regra de importação? Suba a versão para o Unity reimportar.
        public override uint GetVersion() => 1;

        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Roteiro.PastaVozes + "/")) return;

            var importador = (AudioImporter)assetImporter;
            importador.forceToMono = true;
            importador.loadInBackground = true;
            importador.defaultSampleSettings = new AudioImporterSampleSettings
            {
                loadType = AudioClipLoadType.CompressedInMemory,
                compressionFormat = AudioCompressionFormat.Vorbis,
                quality = 0.6f,
                sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate,
                preloadAudioData = true
            };
        }
    }
}

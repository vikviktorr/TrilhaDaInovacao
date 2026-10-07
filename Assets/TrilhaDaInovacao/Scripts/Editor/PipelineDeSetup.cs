using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TrilhaDaInovacao.EditorTools
{
    /// <summary>
    /// Pontos de entrada do menu e do -executeMethod (ver Ferramentas/build.sh):
    /// importar o TMP, configurar XR + gerar cenas, e gerar os builds.
    /// </summary>
    public static class PipelineDeSetup
    {
        const string PastaBuilds = "Builds";
        const string CaminhoIcone = "Assets/TrilhaDaInovacao/Gerado/Icone.png";

        /// <summary>
        /// O TextMeshPro precisa dos "Essential Resources" (fonte padrão e
        /// shaders). Roda numa chamada própria do Unity, antes de gerar cenas.
        /// </summary>
        [MenuItem("Trilha da Inovação/Importar TMP Essentials")]
        public static void ImportarTMP()
        {
            if (File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
            {
                Debug.Log("[Trilha da Inovação] TMP Essentials já importado.");
                return;
            }

            var pacote = Path.GetFullPath("Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage");
            AssetDatabase.ImportPackage(pacote, false);
            AssetDatabase.Refresh();
            Debug.Log("[Trilha da Inovação] TMP Essentials importado.");
        }

        [MenuItem("Trilha da Inovação/0 · Configurar tudo (XR + cenas)")]
        public static void Tudo()
        {
            ConfiguradorXR.Configurar();
            Icone.GerarEAplicar(CaminhoIcone);
            ConstrutorDeCenas.ConstruirTudo();
            AssetDatabase.SaveAssets();
            Debug.Log("[Trilha da Inovação] Setup completo.");
        }

        [MenuItem("Trilha da Inovação/3 · Build Linux")]
        public static void BuildLinux() =>
            Gerar(BuildTarget.StandaloneLinux64, $"{PastaBuilds}/Linux/TrilhaDaInovacao.x86_64");

        [MenuItem("Trilha da Inovação/3 · Build Windows")]
        public static void BuildWindows() =>
            Gerar(BuildTarget.StandaloneWindows64, $"{PastaBuilds}/Windows/TrilhaDaInovacao.exe");

        [MenuItem("Trilha da Inovação/3 · Build Quest 2 (APK)")]
        public static void BuildQuest()
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            Gerar(BuildTarget.Android, $"{PastaBuilds}/Android/TrilhaDaInovacao.apk");
        }

        static void Gerar(BuildTarget alvo, string saida)
        {
            var cenas = EditorBuildSettings.scenes.Where(c => c.enabled).Select(c => c.path).ToArray();
            if (cenas.Length == 0)
            {
                Falhar("Scene List vazia: rode Tudo() antes do build.");
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(saida) ?? PastaBuilds);
            var relatorio = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = cenas,
                target = alvo,
                locationPathName = saida,
                options = BuildOptions.None
            });

            var resumo = relatorio.summary;
            if (resumo.result != BuildResult.Succeeded)
            {
                Falhar($"Build {alvo} falhou: {resumo.result}, {resumo.totalErrors} erro(s).");
                return;
            }

            Debug.Log($"[Trilha da Inovação] Build {alvo} pronto em {saida} " +
                      $"({resumo.totalSize / (1024f * 1024f):0.0} MB, {resumo.totalTime.TotalSeconds:0} s).");
        }

        static void Falhar(string mensagem)
        {
            Debug.LogError($"[Trilha da Inovação] {mensagem}");
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}

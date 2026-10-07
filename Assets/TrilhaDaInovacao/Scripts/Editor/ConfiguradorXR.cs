using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

namespace TrilhaDaInovacao.EditorTools
{
    /// <summary>
    /// Liga o OpenXR (PC + Quest 2 via Android) e aplica as configurações de
    /// player do guia de produção (Quest 2: Android, ARM64, IL2CPP, Linear).
    /// </summary>
    public static class ConfiguradorXR
    {
        const string PastaXR = "Assets/XR";
        const string CaminhoSettings = PastaXR + "/XRGeneralSettings.asset";
        const string LoaderOpenXR = "UnityEngine.XR.OpenXR.OpenXRLoader";

        public const string Versao = "1.0.0";
        public const int CodigoVersao = 1;

        [MenuItem("Trilha da Inovação/1 · Configurar XR (OpenXR + Quest 2)")]
        public static void Configurar()
        {
            var perBuildTarget = ObterOuCriarSettings();

            AtivarOpenXR(perBuildTarget, BuildTargetGroup.Standalone);

            if (AndroidDisponivel)
            {
                AtivarOpenXR(perBuildTarget, BuildTargetGroup.Android);
            }
            else
            {
                // Sem o módulo, o Unity não cria as features OpenXR do Android:
                // o "Meta Quest Support" ficaria faltando em silêncio.
                Debug.LogWarning(
                    "[Trilha da Inovação] Android Build Support não está instalado neste editor. " +
                    "As configurações de PC VR foram aplicadas, mas o build para o Quest 2 exige " +
                    "instalar o módulo pelo Unity Hub (Installs > 6000.0.83f1 > Add modules > " +
                    "Android Build Support, com OpenJDK e Android SDK/NDK) e rodar este menu de novo.");
            }

            AplicarPlayerSettings();

            EditorUtility.SetDirty(perBuildTarget);
            AssetDatabase.SaveAssets();
            Debug.Log("[Trilha da Inovação] OpenXR configurado para Standalone e Android (Quest 2).");
        }

        static XRGeneralSettingsPerBuildTarget ObterOuCriarSettings()
        {
            EditorBuildSettings.TryGetConfigObject(
                XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget perBuildTarget);

            if (perBuildTarget != null) return perBuildTarget;

            if (!Directory.Exists(PastaXR)) Directory.CreateDirectory(PastaXR);

            perBuildTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(perBuildTarget, CaminhoSettings);
            AssetDatabase.SaveAssets();
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perBuildTarget, true);

            return perBuildTarget;
        }

        static void AtivarOpenXR(XRGeneralSettingsPerBuildTarget perBuildTarget, BuildTargetGroup grupo)
        {
            var settings = perBuildTarget.SettingsForBuildTarget(grupo);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<XRGeneralSettings>();
                settings.name = $"{grupo} Settings";
                AssetDatabase.AddObjectToAsset(settings, perBuildTarget);
                perBuildTarget.SetSettingsForBuildTarget(grupo, settings);
            }

            if (settings.Manager == null)
            {
                var manager = ScriptableObject.CreateInstance<XRManagerSettings>();
                manager.name = $"{grupo} Providers";
                AssetDatabase.AddObjectToAsset(manager, perBuildTarget);
                settings.Manager = manager;
            }

            // No PC o XRStartup inicia o OpenXR à mão, para cair no modo
            // desktop quando não há headset; no Quest o sistema já inicia.
            settings.InitManagerOnStart = grupo == BuildTargetGroup.Android;
            XRPackageMetadataStore.AssignLoader(settings.Manager, LoaderOpenXR, grupo);

            AtivarFeaturesOpenXR(grupo);
        }

        /// <summary>
        /// Liga o suporte Meta Quest e o perfil de interação do Touch sem
        /// depender dos tipos concretos (que mudam de nome entre versões).
        /// </summary>
        static void AtivarFeaturesOpenXR(BuildTargetGroup grupo)
        {
            FeatureHelpers.RefreshFeatures(grupo);

            var openXR = OpenXRSettings.GetSettingsForBuildTargetGroup(grupo);
            if (openXR == null) return;

            foreach (var feature in openXR.GetFeatures())
            {
                if (feature == null) continue;
                var tipo = feature.GetType().FullName ?? string.Empty;

                var querido =
                    tipo.Contains("OculusTouchControllerProfile") ||
                    tipo.EndsWith("Hands.OpenXR.HandTracking") ||
                    tipo.Contains("MetaHandTrackingAim") ||
                    (grupo == BuildTargetGroup.Android && tipo.Contains("MetaQuestFeature"));

                if (querido) feature.enabled = true;
            }

            EditorUtility.SetDirty(openXR);
        }

        static bool AndroidDisponivel =>
            BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android);

        static void AplicarPlayerSettings()
        {
            PlayerSettings.companyName = "Fofucho's Games Group";
            PlayerSettings.productName = "Trilha da Inovação";
            PlayerSettings.bundleVersion = Versao;
            PlayerSettings.Android.bundleVersionCode = CodigoVersao;

            // PC: janela redimensionável (modo desktop e apresentação).
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;

            // Linear + Vulkan: recomendação de desempenho para o Quest 2 com URP.
            PlayerSettings.colorSpace = ColorSpace.Linear;

            PlayerSettings.SetApplicationIdentifier(
                NamedBuildTarget.Standalone, "com.fofuchosgames.trilhadainovacao");

            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,
                new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            // OpenXR no Quest com a entrada clássica (Activity).
            PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;
            PlayerSettings.SetApplicationIdentifier(
                NamedBuildTarget.Android, "com.fofuchosgames.trilhadainovacao");

            // Sem locomoção artificial: nada de vinheta ou snap turn a configurar.
            QualitySettings.vSyncCount = 0;
        }
    }
}

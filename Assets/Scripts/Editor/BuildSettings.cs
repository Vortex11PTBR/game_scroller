/*
 * BuildSettings.cs — Utilitário de build para publicação na Google Play Store
 * Propósito: Configura automaticamente todas as PlayerSettings necessárias para Play Store
 *            e executa builds de release e debug via menu do Unity Editor.
 * Como usar: Menus disponíveis em GameScroller > Build > e GameScroller > Setup >
 * Dependências: Apenas APIs do UnityEditor (não vai para o build final)
 *
 * ATENÇÃO: Este arquivo está em Assets/Scripts/Editor/ e não é incluído no build do app.
 */

#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GameScroller.Editor
{
    public static class BuildSettings
    {
        // -----------------------------------------------------------------------
        // Configurações do App
        // -----------------------------------------------------------------------

        private const string BundleIdentifier  = "com.SEUNOME.gamescroller"; // SUBSTITUIR
        private const string AppVersion         = "1.0.0";
        private const int    BundleVersionCode  = 1;
        private const string ProductName        = "Game Scroller";
        private const string CompanyName        = "Seu Nome"; // SUBSTITUIR

        // Pasta de saída dos builds
        private const string BuildOutputPath    = "Builds/Android";
        private const string ReleaseBuildName   = "GameScroller-release.aab";
        private const string DebugBuildName     = "GameScroller-debug.apk";

        // -----------------------------------------------------------------------
        // Menu: Configurar Player Settings
        // -----------------------------------------------------------------------

        [MenuItem("GameScroller/Setup/Configure Player Settings", priority = 1)]
        public static void ConfigurePlayerSettings()
        {
            Debug.Log("[BuildSettings] Configurando Player Settings para Play Store...");

            // Identificação do app
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, BundleIdentifier);
            PlayerSettings.productName          = ProductName;
            PlayerSettings.companyName          = CompanyName;
            PlayerSettings.bundleVersion        = AppVersion;
            PlayerSettings.Android.bundleVersionCode = BundleVersionCode;

            // Orientação — apenas portrait (vertical), conforme AndroidManifest
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait            = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown  = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft       = false;
            PlayerSettings.allowedAutorotateToLandscapeRight      = false;

            // Backend de scripting: IL2CPP (obrigatório para Play Store)
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);

            // Arquitetura: ARM64 (obrigatório para Play Store desde agosto/2019)
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // API Compatibility Level: .NET Standard 2.1
            PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Android, ApiCompatibilityLevel.NET_Standard_2_0);

            // SDK mínimo: API 22 (Android 5.1)
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel22;

            // SDK alvo: API 34 (Android 14) — obrigatório pela Play Store
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;

            // Otimizações de build
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.Minimal);

            // Acesso à internet: obrigatório para anúncios e IAP
            PlayerSettings.Android.forceInternetPermission = true;

            // Unsafe code: desabilitado por padrão (segurança)
            PlayerSettings.allowUnsafeCode = false;

            Debug.Log("[BuildSettings] ✅ Player Settings configuradas com sucesso!");
            Debug.Log($"  Bundle ID:     {BundleIdentifier}");
            Debug.Log($"  Versão:        {AppVersion} (code {BundleVersionCode})");
            Debug.Log($"  Backend:       IL2CPP");
            Debug.Log($"  Arquitetura:   ARM64");
            Debug.Log($"  Min API:       22 (Android 5.1)");
            Debug.Log($"  Target API:    34 (Android 14)");

            EditorUtility.DisplayDialog(
                "Game Scroller — Build Settings",
                "✅ Player Settings configuradas!\n\n" +
                "Próximos passos:\n" +
                "1. Configure o Keystore em Player Settings > Publishing Settings\n" +
                "2. Substitua os IDs do AdMob no AdManager\n" +
                "3. Execute: GameScroller > Build > Android Release",
                "OK"
            );
        }

        // -----------------------------------------------------------------------
        // Menu: Build de Release (AAB para Play Store)
        // -----------------------------------------------------------------------

        [MenuItem("GameScroller/Build/Android Release (.aab)", priority = 10)]
        public static void BuildAndroidRelease()
        {
            Debug.Log("[BuildSettings] Iniciando build de RELEASE para Play Store...");

            // Valida configurações antes de buildar
            if (!ValidateBuildSettings())
                return;

            ConfigurePlayerSettings();

            // App Bundle (.aab) — obrigatório para upload na Play Store
            EditorUserBuildSettings.buildAppBundle = true;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            EditorUserBuildSettings.development = false;

            string outputPath = EnsureOutputPath(BuildOutputPath);
            string buildPath  = Path.Combine(outputPath, ReleaseBuildName);

            var buildOptions = new BuildPlayerOptions
            {
                scenes            = GetEnabledScenes(),
                locationPathName  = buildPath,
                target            = BuildTarget.Android,
                options           = BuildOptions.None
            };

            ExecuteBuild(buildOptions, buildPath, "Release");
        }

        // -----------------------------------------------------------------------
        // Menu: Build de Debug (APK para testes)
        // -----------------------------------------------------------------------

        [MenuItem("GameScroller/Build/Android Debug (.apk)", priority = 11)]
        public static void BuildAndroidDebug()
        {
            Debug.Log("[BuildSettings] Iniciando build de DEBUG...");

            // APK para instalar diretamente no dispositivo (não vai para Play Store)
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            EditorUserBuildSettings.development = true;
            EditorUserBuildSettings.connectProfiler = false;

            string outputPath = EnsureOutputPath(BuildOutputPath);
            string buildPath  = Path.Combine(outputPath, DebugBuildName);

            var buildOptions = new BuildPlayerOptions
            {
                scenes            = GetEnabledScenes(),
                locationPathName  = buildPath,
                target            = BuildTarget.Android,
                options           = BuildOptions.Development | BuildOptions.AllowDebugging
            };

            ExecuteBuild(buildOptions, buildPath, "Debug");
        }

        // -----------------------------------------------------------------------
        // Menu: Abrir pasta de builds
        // -----------------------------------------------------------------------

        [MenuItem("GameScroller/Build/Abrir Pasta de Builds", priority = 30)]
        public static void OpenBuildsFolder()
        {
            string fullPath = Path.GetFullPath(BuildOutputPath);
            EnsureOutputPath(BuildOutputPath);
            EditorUtility.RevealInFinder(fullPath);
        }

        // -----------------------------------------------------------------------
        // Auxiliares internos
        // -----------------------------------------------------------------------

        private static void ExecuteBuild(BuildPlayerOptions options, string buildPath, string buildType)
        {
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                double sizeInMB = summary.totalSize / (1024.0 * 1024.0);
                Debug.Log($"[BuildSettings] ✅ Build {buildType} concluída com sucesso!");
                Debug.Log($"  Arquivo:   {buildPath}");
                Debug.Log($"  Tamanho:   {sizeInMB:F1} MB");
                Debug.Log($"  Duração:   {summary.totalTime.TotalSeconds:F1}s");

                EditorUtility.DisplayDialog(
                    $"Build {buildType} Concluída!",
                    $"✅ Build concluída com sucesso!\n\n" +
                    $"Arquivo: {Path.GetFileName(buildPath)}\n" +
                    $"Tamanho: {sizeInMB:F1} MB\n\n" +
                    (buildType == "Release"
                        ? "Próximo passo: faça upload do .aab no Google Play Console."
                        : "Instale o .apk no dispositivo via ADB ou manualmente."),
                    "OK"
                );
            }
            else
            {
                Debug.LogError($"[BuildSettings] ❌ Build {buildType} falhou! Resultado: {summary.result}");
                Debug.LogError($"  Erros: {summary.totalErrors}");
                EditorUtility.DisplayDialog(
                    $"Build {buildType} Falhou",
                    $"❌ A build falhou com {summary.totalErrors} erro(s).\n\n" +
                    "Verifique o Console para detalhes.",
                    "OK"
                );
            }
        }

        /// <summary>
        /// Retorna a lista de cenas habilitadas em Build Settings.
        /// </summary>
        private static string[] GetEnabledScenes()
        {
            var scenes = EditorBuildSettings.scenes;
            var enabledScenes = System.Array.FindAll(scenes, s => s.enabled);

            if (enabledScenes.Length == 0)
            {
                Debug.LogWarning("[BuildSettings] Nenhuma cena habilitada em File > Build Settings!");
                EditorUtility.DisplayDialog(
                    "Nenhuma Cena",
                    "Adicione pelo menos uma cena em File > Build Settings antes de buildar.",
                    "OK"
                );
                return Array.Empty<string>();
            }

            var paths = new string[enabledScenes.Length];
            for (int i = 0; i < enabledScenes.Length; i++)
                paths[i] = enabledScenes[i].path;

            return paths;
        }

        /// <summary>
        /// Valida as configurações essenciais antes de iniciar o build.
        /// </summary>
        private static bool ValidateBuildSettings()
        {
            // Verifica se o Bundle Identifier foi personalizado
            string currentId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
            if (currentId.Contains("SEUNOME") || string.IsNullOrEmpty(currentId))
            {
                bool proceed = EditorUtility.DisplayDialog(
                    "Bundle Identifier Padrão",
                    $"O Bundle Identifier ainda está no padrão:\n'{currentId}'\n\n" +
                    "Substitua 'SEUNOME' pelo seu identificador único antes de publicar na Play Store.\n\n" +
                    "Deseja continuar mesmo assim?",
                    "Continuar",
                    "Cancelar"
                );
                if (!proceed) return false;
            }

            return true;
        }

        /// <summary>
        /// Garante que a pasta de saída existe e retorna o caminho completo.
        /// </summary>
        private static string EnsureOutputPath(string relativePath)
        {
            string fullPath = Path.GetFullPath(relativePath);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                Debug.Log($"[BuildSettings] Pasta criada: {fullPath}");
            }
            return fullPath;
        }
    }
}
#endif

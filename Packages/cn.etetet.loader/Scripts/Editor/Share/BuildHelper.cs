using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using BuildReport = UnityEditor.Build.Reporting.BuildReport;
using BuildResult = UnityEditor.Build.Reporting.BuildResult;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;

namespace ET
{
    public static class BuildHelper
    {
        private const string relativeDirPrefix = "./Release";

        public static string BuildFolder = "./Release/{0}/StreamingAssets/";

        [MenuItem("ET/Loader/Build macOS", false)]
        public static void BuildMacOS()
        {
            Build(PlatformType.MacOS, BuildOptions.None);
        }

        [MenuItem("ET/Loader/Build iOS", false)]
        public static void BuildIOS()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
            {
                throw new BuildFailedException("Switch to iOS and regenerate HybridCLR DLLs and resource bundles before building.");
            }

            Build(PlatformType.IOS, BuildOptions.None);
        }

        [MenuItem("ET/Loader/Build Bundles (Keep Existing Files)", false)]
        public static void BuildResourceBundles()
        {
            AssetDatabase.Refresh();
            AssetBundleCollectorSetting setting = AssetBundleCollectorSettingData.Setting;
            string pipelineName = nameof(EBuildPipeline.ScriptableBuildPipeline);
            string packageVersion = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");
            foreach (AssetBundleCollectorPackage package in setting.Packages)
            {
                string packageName = package.PackageName;
                ScriptableBuildParameters parameters = new()
                {
                    BuildOutputRoot = AssetBundleBuilderHelper.GetDefaultBuildOutputRoot(),
                    BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot(),
                    BuildPipeline = pipelineName,
                    BuildBundleType = (int)EBuildBundleType.AssetBundle,
                    BuildTarget = EditorUserBuildSettings.activeBuildTarget,
                    PackageName = packageName,
                    PackageVersion = packageVersion,
                    EnableSharePackRule = true,
                    VerifyBuildingResult = true,
                    FileNameStyle = AssetBundleBuilderSetting.GetPackageFileNameStyle(packageName, pipelineName),
                    CompressOption = AssetBundleBuilderSetting.GetPackageCompressOption(packageName, pipelineName),
                    UseAssetDependencyDB = AssetBundleBuilderSetting.GetPackageUseAssetDependencyDB(packageName, pipelineName),
                    ClearBuildCacheFiles = false,
                    BuildinFileCopyOption = EBuildinFileCopyOption.OnlyCopyAll,
                    EncryptionServices = CreateBuildService<IEncryptionServices>(AssetBundleBuilderSetting.GetPackageEncyptionServicesClassName(packageName, pipelineName)),
                    ManifestProcessServices = CreateBuildService<IManifestProcessServices>(AssetBundleBuilderSetting.GetPackageManifestProcessServicesClassName(packageName, pipelineName)),
                    ManifestRestoreServices = CreateBuildService<IManifestRestoreServices>(AssetBundleBuilderSetting.GetPackageManifestRestoreServicesClassName(packageName, pipelineName)),
                    BuiltinShadersBundleName = DefaultPackRule.CreateShadersPackRuleResult().GetBundleName(packageName, setting.UniqueBundleName),
                };
                var result = new ScriptableBuildPipeline().Run(parameters, true);
                if (!result.Success)
                {
                    throw new BuildFailedException($"Resource build failed: {result.FailedTask}: {result.ErrorInfo}");
                }

                Debug.Log($"Built resource package {packageName}: {packageVersion}");
            }
        }

        private static T CreateBuildService<T>(string className) where T : class
        {
            Type serviceType = YooAsset.Editor.EditorTools.GetAssignableTypes(typeof(T)).Find(type => type.FullName == className);
            if (serviceType == null)
            {
                throw new BuildFailedException($"Resource build service not found: {className}");
            }

            return (T)Activator.CreateInstance(serviceType);
        }

#if ENABLE_VIEW
        [MenuItem("ET/Loader/Remove ENABLE_VIEW", false)]
        public static void RemoveEnableView()
        {
            EnableDefineSymbols("ENABLE_VIEW", false);
        }
#else
        [MenuItem("ET/Loader/Add ENABLE_VIEW", false)]
        public static void AddEnableView()
        {
            EnableDefineSymbols("ENABLE_VIEW", true);
        }
#endif
        public static void EnableDefineSymbols(string symbols, bool enable)
        {
            Debug.Log($"EnableDefineSymbols {symbols} {enable}");
            string defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup));
            var ss = defines.Split(';').ToList();
            if (enable)
            {
                if (ss.Contains(symbols))
                {
                    return;
                }

                ss.Add(symbols);
            }
            else
            {
                if (!ss.Contains(symbols))
                {
                    return;
                }

                ss.Remove(symbols);
            }

            Debug.Log($"EnableDefineSymbols {symbols} {enable}");
            defines = string.Join(";", ss);
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup), defines);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void Build(PlatformType type, BuildOptions buildOptions)
        {
            BuildTarget buildTarget = BuildTarget.StandaloneWindows;
            string programName = "ET";
            string exeName = programName;
            switch (type)
            {
                case PlatformType.Windows:
                    buildTarget = BuildTarget.StandaloneWindows64;
                    exeName += ".exe";
                    break;
                case PlatformType.Android:
                    buildTarget = BuildTarget.Android;
                    exeName += ".apk";
                    break;
                case PlatformType.IOS:
                    buildTarget = BuildTarget.iOS;
                    exeName = Path.Combine("iOS", programName);
                    break;
                case PlatformType.MacOS:
                    buildTarget = BuildTarget.StandaloneOSX;
                    exeName += ".app";
                    break;
                case PlatformType.Linux:
                    buildTarget = BuildTarget.StandaloneLinux64;
                    break;
                case PlatformType.WebGL:
                    buildTarget = BuildTarget.WebGL;
                    break;
            }

            AssetDatabase.Refresh();

            Debug.Log("start build");

            string[] levels = EditorBuildSettings.scenes
                    .Where(scene => scene.enabled)
                    .Select(scene => scene.path)
                    .ToArray();
            if (levels.Length == 0 || levels.Any(path => !File.Exists(path)))
            {
                throw new BuildFailedException("Build Settings must contain enabled scenes with valid paths.");
            }

            GlobalConfig globalConfig = Resources.Load<GlobalConfig>(nameof(GlobalConfig));
            if (globalConfig == null || globalConfig.CodeMode != CodeMode.Client)
            {
                throw new BuildFailedException("Build package requires GlobalConfig with CodeMode.Client.");
            }

            BuildReport report = BuildPipeline.BuildPlayer(levels, $"{relativeDirPrefix}/{exeName}", buildTarget, buildOptions);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"BuildResult:{report.summary.result}");
            }

            Debug.Log("finish build");
            EditorUtility.OpenWithDefaultApp(relativeDirPrefix);
        }
    }
}

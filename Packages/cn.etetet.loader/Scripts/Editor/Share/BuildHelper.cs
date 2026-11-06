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
    /// <summary>
    /// 客户端应用打包与 YooAsset 资源包构建辅助类。
    /// 提供 macOS/iOS 应用打包菜单、YooAsset AssetBundle 增量构建菜单以及宏定义开关菜单。
    /// </summary>
    public static class BuildHelper
    {
        /// <summary>打包输出根目录（<c>./Release</c>）。</summary>
        private const string relativeDirPrefix = "./Release";

        public static string BuildFolder = "./Release/{0}/StreamingAssets/";

        /// <summary>
        /// 菜单项 <c>ET/Loader/Build macOS</c>：一键构建 macOS 客户端应用（输出至 <c>Release/ET.app</c>）。
        /// </summary>
        [MenuItem("ET/Loader/Build macOS", false)]
        public static void BuildMacOS()
        {
            Build(PlatformType.MacOS, BuildOptions.None);
        }

        /// <summary>
        /// 菜单项 <c>ET/Loader/Build iOS</c>：构建 iOS Xcode 工程（输出至 <c>Release/iOS/ET</c>）。
        /// </summary>
        [MenuItem("ET/Loader/Build iOS", false)]
        public static void BuildIOS()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
            {
                throw new BuildFailedException("Switch to iOS and regenerate HybridCLR DLLs and resource bundles before building.");
            }

            Build(PlatformType.IOS, BuildOptions.None);
        }

        /// <summary>
        /// 菜单项 <c>ET/Loader/Build Bundles (Keep Existing Files)</c>：
        /// 使用 YooAsset 的 <see cref="ScriptableBuildPipeline"/> 构建所有配置的资源包并复制到 <c>StreamingAssets</c>（保留已有文件）。
        /// </summary>
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

        /// <summary>
        /// 根据类名反射创建 YooAsset 构建阶段所需的加密或清单处理服务实例。
        /// </summary>
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
        /// <summary>
        /// 移除 <c>ENABLE_VIEW</c> 编译宏（关闭 Hierarchy 实体视图调试功能）。
        /// </summary>
        [MenuItem("ET/Loader/Remove ENABLE_VIEW", false)]
        public static void RemoveEnableView()
        {
            EnableDefineSymbols("ENABLE_VIEW", false);
        }
#else
        /// <summary>
        /// 添加 <c>ENABLE_VIEW</c> 编译宏（开启 Hierarchy 实体视图调试功能）。
        /// </summary>
        [MenuItem("ET/Loader/Add ENABLE_VIEW", false)]
        public static void AddEnableView()
        {
            EnableDefineSymbols("ENABLE_VIEW", true);
        }
#endif
        /// <summary>
        /// 为当前选中的构建目标平台添加或移除指定的脚本编译宏（Scripting Define Symbols）。
        /// </summary>
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

        /// <summary>
        /// 执行指定平台与构建选项的 Unity Player 打包，输出到 <c>./Release</c> 目录并在完成后打开输出文件夹。
        /// </summary>
        /// <param name="type">目标平台枚举。</param>
        /// <param name="buildOptions">Unity 构建选项标志位。</param>
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

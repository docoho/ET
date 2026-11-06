#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace ET
{
    /// <summary>
    /// iOS 打包后处理器（<see cref="PostProcessBuildAttribute"/>）。
    /// 在 Unity 导出 Xcode 工程后，自动复制自定义 <c>ETUnitySceneDelegate.mm</c> 源码文件到 Xcode 工程中，
    /// 并配置 <c>Info.plist</c> 的 SceneManifest 及本地局域网访问权限描述（<c>NSLocalNetworkUsageDescription</c>）。
    /// </summary>
    public static class IOSBuildPostprocessor
    {
        private const string SceneDelegateTemplate = "Packages/cn.etetet.loader/Templates/iOS/ETUnitySceneDelegate.mm.txt";
        private const string SceneDelegateProjectPath = "Libraries/ETUnitySceneDelegate.mm";

        /// <summary>
        /// iOS 构建完成后自动执行的 Xcode 工程与 <c>Info.plist</c> 配置回调。
        /// </summary>
        /// <param name="target">构建目标平台。</param>
        /// <param name="outputPath">Xcode 工程导出根目录路径。</param>
        [PostProcessBuild(1000)]
        public static void ConfigureIOS(BuildTarget target, string outputPath)
        {
            if (target != BuildTarget.iOS)
            {
                return;
            }

            // 复制自定义 SceneDelegate 模板到 Xcode 工程目录
            string nativePath = Path.Combine(outputPath, SceneDelegateProjectPath);
            Directory.CreateDirectory(Path.GetDirectoryName(nativePath));
            File.Copy(SceneDelegateTemplate, nativePath, true);

            // 将 ETUnitySceneDelegate.mm 添加到 UnityFramework Target 的编译文件列表中
            string projectPath = PBXProject.GetPBXProjectPath(outputPath);
            PBXProject project = new();
            project.ReadFromFile(projectPath);
            string fileGuid = project.FindFileGuidByProjectPath(SceneDelegateProjectPath);
            if (string.IsNullOrEmpty(fileGuid))
            {
                fileGuid = project.AddFile(SceneDelegateProjectPath, SceneDelegateProjectPath, PBXSourceTree.Source);
                project.AddFileToBuild(project.GetUnityFrameworkTargetGuid(), fileGuid);
            }
            project.WriteToFile(projectPath);

            // 修改 Info.plist：配置 UIApplicationSceneManifest 与局域网通信权限说明
            string plistPath = Path.Combine(outputPath, "Info.plist");
            PlistDocument plist = new();
            plist.ReadFromFile(plistPath);
            PlistElementDict manifest = GetOrCreateDict(plist.root, "UIApplicationSceneManifest");
            manifest.SetBoolean("UIApplicationSupportsMultipleScenes", false);
            PlistElementDict configurations = GetOrCreateDict(manifest, "UISceneConfigurations");
            PlistElementDict scene = configurations.CreateArray("UIWindowSceneSessionRoleApplication").AddDict();
            scene.SetString("UISceneClassName", "UIWindowScene");
            scene.SetString("UISceneConfigurationName", "Unity");
            scene.SetString("UISceneDelegateClassName", Path.GetFileNameWithoutExtension(SceneDelegateProjectPath));
            plist.root.SetString("NSLocalNetworkUsageDescription", "用于连接局域网中的游戏服务器。");
            plist.WriteToFile(plistPath);
        }

        /// <summary>
        /// 从父 Plist 字典中获取指定键的子字典，若不存在则自动创建。
        /// </summary>
        private static PlistElementDict GetOrCreateDict(PlistElementDict parent, string key)
        {
            return parent.values.TryGetValue(key, out PlistElement value) ? value.AsDict() : parent.CreateDict(key);
        }
    }
}
#endif

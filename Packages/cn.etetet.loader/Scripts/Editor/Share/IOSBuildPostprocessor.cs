#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace ET
{
    public static class IOSBuildPostprocessor
    {
        private const string SceneDelegateTemplate = "Packages/cn.etetet.loader/Templates/iOS/ETUnitySceneDelegate.mm.txt";
        private const string SceneDelegateProjectPath = "Libraries/ETUnitySceneDelegate.mm";

        [PostProcessBuild(1000)]
        public static void ConfigureIOS(BuildTarget target, string outputPath)
        {
            if (target != BuildTarget.iOS)
            {
                return;
            }

            string nativePath = Path.Combine(outputPath, SceneDelegateProjectPath);
            Directory.CreateDirectory(Path.GetDirectoryName(nativePath));
            File.Copy(SceneDelegateTemplate, nativePath, true);

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

        private static PlistElementDict GetOrCreateDict(PlistElementDict parent, string key)
        {
            return parent.values.TryGetValue(key, out PlistElement value) ? value.AsDict() : parent.CreateDict(key);
        }
    }
}
#endif

using UnityEditor;
using UnityEngine;
using YooAsset;

namespace ET
{
    /// <summary>
    /// 客户端打包目标平台枚举。
    /// </summary>
    public enum PlatformType
    {
        None,
        Android,
        IOS,
        Windows,
        MacOS,
        Linux,
        WebGL
    }


    /// <summary>
    /// 客户端构建工具编辑器窗口（菜单项：<c>ET/Loader/Build Tool</c>）。
    /// 提供可视化的目标平台（<see cref="PlatformType"/>）与构建选项（<see cref="BuildOptions"/>）选择，并调用 <see cref="BuildHelper.Build"/> 执行客户端打包。
    /// </summary>
    public class BuildEditor : EditorWindow
    {
        private PlatformType activePlatform;
        private PlatformType platformType;
        private BuildOptions buildOptions;

        private GlobalConfig globalConfig;
        private YooConfig yooConfig;

        /// <summary>
        /// 打开客户端构建工具窗口。
        /// </summary>
        [MenuItem("ET/Loader/Build Tool", false)]
        public static void ShowWindow()
        {
            GetWindow<BuildEditor>();
        }

        /// <summary>
        /// 窗口激活时加载 <see cref="GlobalConfig"/> 与 <see cref="YooConfig"/>，并识别当前编辑器所处的激活平台。
        /// </summary>
        private void OnEnable()
        {
            globalConfig = Resources.Load<GlobalConfig>(nameof(GlobalConfig));
            yooConfig = Resources.Load<YooConfig>(nameof(YooConfig));

#if UNITY_ANDROID
            activePlatform = PlatformType.Android;
#elif UNITY_IOS
            activePlatform = PlatformType.IOS;
#elif UNITY_STANDALONE_WIN
            activePlatform = PlatformType.Windows;
#elif UNITY_STANDALONE_OSX
            activePlatform = PlatformType.MacOS;
#elif UNITY_STANDALONE_LINUX
            activePlatform = PlatformType.Linux;
#elif UNITY_WEBGL
            activePlatform = PlatformType.WebGL;
#else
            activePlatform = PlatformType.None;
#endif
            platformType = activePlatform;
        }

        /// <summary>
        /// 绘制打包工具窗口 GUI，校验 <see cref="GlobalConfig.CodeMode"/> 必须为 <see cref="CodeMode.Client"/> 后触发打包。
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.LabelField("PlatformType ");
            this.platformType = (PlatformType)EditorGUILayout.EnumPopup(platformType);

            EditorGUILayout.LabelField("BuildOptions ");
            this.buildOptions = (BuildOptions)EditorGUILayout.EnumFlagsField(this.buildOptions);

            GUILayout.Space(5);

            if (GUILayout.Button("BuildPackage"))
            {
                if (this.platformType == PlatformType.None)
                {
                    Log.Error("please select platform!");
                    return;
                }

                // 客户端打包要求 CodeMode 必须先切换为 Client，避免将服务端代码打入客户端包
                if (this.globalConfig.CodeMode != CodeMode.Client)
                {
                    Log.Error("build package CodeMode must be CodeMode.Client, please select Client");
                    return;
                }

                if (platformType != activePlatform)
                {
                    switch (EditorUtility.DisplayDialogComplex("Warning!", $"current platform is {activePlatform}, if change to {platformType}, may be take a long time", "change", "cancel", "no change"))
                    {
                        case 0:
                            activePlatform = platformType;
                            break;
                        case 1:
                            return;
                        case 2:
                            platformType = activePlatform;
                            break;
                    }
                }

                BuildHelper.Build(this.platformType, this.buildOptions);
                return;
            }

            GUILayout.Space(5);
        }
    }
}

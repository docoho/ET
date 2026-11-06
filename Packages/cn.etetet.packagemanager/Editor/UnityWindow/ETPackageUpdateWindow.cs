using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ET.PackageManager.Editor
{
    /// <summary>
    /// ET包管理 更新检测窗口（无 Odin Inspector 环境下的原生 IMGUI 回退窗口）
    /// </summary>
    public class ETPackageUpdateWindow : EditorWindow
    {
        #if !ODIN_INSPECTOR
        [MenuItem("ET/ETPackage 更新检查")]
        #endif
        private static void OpenWindow()
        {
            var window = GetWindow<ETPackageUpdateWindow>();
            if (window != null)
                window.Show();
        }

        //[MenuItem("ET/关闭ETPackage 更新检查")]
        //错误时使用的 面板出现了错误 会导致如何都打不开 就需要先关闭
        public static void CloseWindow()
        {
            GetWindow<ETPackageUpdateWindow>()?.Close();
        }

        /// <summary>
        /// 格式化后的可更新包展示信息文本
        /// </summary>
        private string m_UpdatePackageInfo;

        /// <summary>
        /// 富文本大号字体 GUI 样式
        /// </summary>
        private GUIStyle m_CustomStyle;

        /// <summary>
        /// 窗口启用时发起所有包的最新版本检查请求
        /// </summary>
        private void OnEnable()
        {
            m_CustomStyle       = new(EditorStyles.wordWrappedMiniLabel) { fontSize = 30, richText = true };
            m_UpdatePackageInfo = "更新信息请求中...";
            PackageHelper.CheckUpdateAll(CheckUpdateAll);
        }

        /// <summary>
        /// 版本检查请求完成回调
        /// </summary>
        private void CheckUpdateAll(bool result)
        {
            if (!result)
            {
                m_UpdatePackageInfo = "更新失败...";
                return;
            }

            ShowUpdateInfo();
        }

        /// <summary>
        /// 比对本地已注册的 cn.etetet.* 包版本与远端最新版本，生成更新提示文本
        /// </summary>
        private void ShowUpdateInfo()
        {
            var sb = SbPool.Get();

            sb.Append("可更新包信息:");
            sb.AppendLine();
            sb.AppendLine();

            var currentETPackageInfo = new Dictionary<string, string>();

            foreach (var packageInfo in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
            {
                var name = packageInfo.name;

                if (name.Contains("cn.etetet."))
                {
                    var version = packageInfo.version;
                    currentETPackageInfo[name] = version;
                }
            }

            var newETPackageInfo = new Dictionary<string, string>();

            foreach (var info in currentETPackageInfo)
            {
                var name       = info.Key;
                var newVersion = PackageHelper.GetPackageLastVersion(name);
                if (string.IsNullOrEmpty(newVersion))
                {
                    continue;
                }

                var version = info.Value;
                if (version != newVersion)
                {
                    newETPackageInfo[name] = newVersion;
                }
            }

            if (newETPackageInfo.Count <= 0)
            {
                sb.Append($"无");
            }
            else
            {
                foreach (var info in newETPackageInfo)
                {
                    var name       = info.Key;
                    var newVersion = info.Value;
                    var version    = currentETPackageInfo[name];

                    sb.Append($"[{name} {version}] >> [<color=#00FF00>{newVersion}</color>]");
                    sb.AppendLine();
                }
            }

            m_UpdatePackageInfo = SbPool.PutAndToStr(sb);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("提示:安装Odin 可使用更详细丰富的管理界面");

            GUILayout.Space(10);
            if (GUILayout.Button("ETPackageManager 包管理文档"))
            {
                Application.OpenURL("https://lib9kmxvq7k.feishu.cn/wiki/DzqwwwBJvixRvtkCI4dcatGcnAd");
            }
            GUILayout.Space(10);
            
            EditorGUILayout.LabelField(m_UpdatePackageInfo, m_CustomStyle);
        }
    }
}

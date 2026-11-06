using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Hibzz.DependencyResolver;
using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// <see cref="GlobalConfig"/> 资产的自定义 Inspector 检视面板。
    /// 支持在 Inspector 中一键根据 <see cref="GlobalConfig.SceneName"/> 切换主包（生成 <c>MainPackage.txt</c> 并硬链接 <c>ET.sln</c>），
    /// 以及在修改 <see cref="GlobalConfig.CodeMode"/> 时自动调用 <c>ET.CodeMode.dll</c> 刷新全工程的 <c>AssemblyReference.asmref</c>。
    /// </summary>
    [CustomEditor(typeof(GlobalConfig))]
    public class GlobalConfigEditor : Editor
    {
        private const string SolutionFileName = "ET.sln";
        private const string TempLinkSuffix = ".mainpackage_link_tmp";

        /// <summary>缓存上一次的 <see cref="CodeMode"/>，用于检测用户在 Inspector 下拉框中是否修改了模式。</summary>
        private CodeMode codeMode;
        
        /// <summary>
        /// 当 Inspector 面板激活时初始化当前 <see cref="CodeMode"/> 状态。
        /// </summary>
        private void OnEnable()
        {
            GlobalConfig globalConfig = (GlobalConfig)this.target;
            this.codeMode = globalConfig.CodeMode;
            //globalConfig.BuildType = EditorUserBuildSettings.development ? BuildType.Debug : BuildType.Release;
            
            EditorUtility.SetDirty(globalConfig);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 绘制自定义 Inspector GUI：在 <c>SceneName</c> 字段下方附加 <c>"Set Main Package"</c> 按钮，并监听 <c>CodeMode</c> 变更。
        /// </summary>
        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();

            GlobalConfig globalConfig = (GlobalConfig)this.target;

            SerializedProperty iterator = this.serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                // 脚本引用字段置灰只读显示
                if (iterator.propertyPath == "m_Script")
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.PropertyField(iterator, true);
                    }

                    continue;
                }

                // 在 SceneName 字段旁提供一键设置主包按钮
                if (iterator.name == nameof(GlobalConfig.SceneName))
                {
                    EditorGUILayout.PropertyField(iterator, true);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Set Main Package"))
                    {
                        SetMainPackageBySceneName(globalConfig);
                    }
                    EditorGUILayout.EndHorizontal();
                    continue;
                }

                EditorGUILayout.PropertyField(iterator, true);
            }

            this.serializedObject.ApplyModifiedProperties();

            // 若用户切换了 CodeMode，立即执行 ET.CodeMode.dll 刷新所有包的程序集引用文件
            if (globalConfig.CodeMode != this.codeMode)
            {
                this.codeMode = globalConfig.CodeMode;
                Process process = ProcessHelper.DotNet($"Bin/ET.CodeMode.dll --CodeMode={globalConfig.CodeMode}", ".", true);
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    UnityEngine.Debug.LogError($"[GlobalConfigEditor] 刷新 AssemblyReference 失败，退出码: {process.ExitCode}");
                }

                AssetDatabase.Refresh();
            }

            EditorUtility.SetDirty(globalConfig);
        }

        /// <summary>
        /// 根据 <see cref="GlobalConfig.SceneName"/> 推导对应的主包名（<c>cn.etetet.{scenename}</c>），
        /// 调用 <see cref="MainPackageSelector.SetAsMainPackage"/> 写入 <c>MainPackage.txt</c>，并将该主包内的 <c>ET.sln</c> 硬链接到工程根目录。
        /// </summary>
        private static void SetMainPackageBySceneName(GlobalConfig globalConfig)
        {
            string sceneName = globalConfig.SceneName?.Trim();
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                UnityEngine.Debug.LogError("[GlobalConfigEditor] SceneName 为空，无法设置主包");
                return;
            }

            string packageName = $"cn.etetet.{sceneName.ToLowerInvariant()}";
            string packageDirectory = Path.Combine("Packages", packageName);
            if (!Directory.Exists(packageDirectory))
            {
                UnityEngine.Debug.LogError($"[GlobalConfigEditor] 包不存在: {packageName}");
                return;
            }

            bool result = MainPackageSelector.SetAsMainPackage(packageName);
            if (!result)
            {
                UnityEngine.Debug.LogError($"[GlobalConfigEditor] 设置主包失败: {packageName}");
                return;
            }

            TryLinkMainPackageSolution(packageName);
        }

        /// <summary>
        /// 将指定主包目录下的 <c>ET.sln</c> 通过先创建临时硬链接再替换的方式链接到工程根目录下的 <c>ET.sln</c>。
        /// </summary>
        private static void TryLinkMainPackageSolution(string packageName)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string sourceSolutionPath = Path.GetFullPath(Path.Combine(projectRoot, "Packages", packageName, SolutionFileName));
            string rootSolutionPath = Path.GetFullPath(Path.Combine(projectRoot, SolutionFileName));
            string tempLinkPath = Path.GetFullPath(Path.Combine(projectRoot, $"{SolutionFileName}{TempLinkSuffix}"));

            if (!File.Exists(sourceSolutionPath))
            {
                UnityEngine.Debug.LogError($"[GlobalConfigEditor] 主包中未找到 {SolutionFileName}: {sourceSolutionPath}");
                return;
            }

            try
            {
                if (!TryDeleteFileIfExists(tempLinkPath, "临时链接文件"))
                {
                    return;
                }

                if (!TryCreateHardLink(tempLinkPath, sourceSolutionPath, projectRoot))
                {
                    UnityEngine.Debug.LogError($"[GlobalConfigEditor] 创建硬连接失败: {sourceSolutionPath} -> {rootSolutionPath}");
                    return;
                }

                if (!TryDeleteFileIfExists(rootSolutionPath, $"项目根目录旧 {SolutionFileName}"))
                {
                    TryDeleteFileIfExists(tempLinkPath, "临时链接文件");
                    return;
                }

                File.Move(tempLinkPath, rootSolutionPath);
                AssetDatabase.Refresh();

                UnityEngine.Debug.Log($"[GlobalConfigEditor] 已将主包 {SolutionFileName} 硬连接到项目根目录: {rootSolutionPath}");
            }
            catch (System.Exception ex)
            {
                TryDeleteFileIfExists(tempLinkPath, "临时链接文件");

                UnityEngine.Debug.LogError($"[GlobalConfigEditor] 链接主包 {SolutionFileName} 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 跨平台创建文件硬链接（Windows 使用 <c>mklink /H</c>，macOS/Linux 使用 <c>ln</c>）。
        /// </summary>
        private static bool TryCreateHardLink(string linkPath, string targetPath, string workingDirectory)
        {
            Process process;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                process = ProcessHelper.Run("cmd.exe", $"/c mklink /H \"{linkPath}\" \"{targetPath}\"", workingDirectory, true);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                string lnExecutable = File.Exists("/bin/ln") ? "/bin/ln" : "/usr/bin/ln";
                process = ProcessHelper.Run(lnExecutable, $"\"{targetPath}\" \"{linkPath}\"", workingDirectory, true);
            }
            else
            {
                UnityEngine.Debug.LogError($"[GlobalConfigEditor] 不支持的操作系统: {RuntimeInformation.OSDescription}");
                return false;
            }

            process.WaitForExit();
            return process.ExitCode == 0;
        }

        /// <summary>
        /// 若文件存在则移除只读属性并将其删除。
        /// </summary>
        private static bool TryDeleteFileIfExists(string filePath, string description)
        {
            if (!File.Exists(filePath))
            {
                return true;
            }

            try
            {
                FileAttributes attributes = File.GetAttributes(filePath);
                if ((attributes & FileAttributes.ReadOnly) != 0)
                {
                    File.SetAttributes(filePath, attributes & ~FileAttributes.ReadOnly);
                }

                File.Delete(filePath);
                UnityEngine.Debug.Log($"[GlobalConfigEditor] 已删除{description}: {filePath}");
                return true;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[GlobalConfigEditor] 删除{description}失败: {filePath}, {ex.Message}");
                return false;
            }
        }
    }
}

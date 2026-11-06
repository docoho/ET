using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// 重新生成 IDE C# 工程文件（<c>.csproj</c>）辅助类。
    /// </summary>
    public static class ReGenerateProjectFilesHelper
    {
        /// <summary>
        /// 在编辑器加载时或通过菜单 <c>ET/Loader/ReGenerateProjectFiles</c> 触发：
        /// 调用当前代码编辑器的 <c>SyncAll()</c> 同步所有 <c>.csproj</c>，并清理前缀为 <c>Ignore.ET*</c> 的占位项目文件。
        /// </summary>
        [InitializeOnLoadMethod]
        [MenuItem("ET/Loader/ReGenerateProjectFiles")]
        public static void Run()
        {
            Unity.CodeEditor.CodeEditor.CurrentEditor.SyncAll();

            foreach (string s in FileHelper.GetAllFiles(".", "Ignore.ET*.csproj"))
            {
                System.IO.File.Delete(s);
            }

            Debug.Log("regenerate csproj");
        }
    }
}
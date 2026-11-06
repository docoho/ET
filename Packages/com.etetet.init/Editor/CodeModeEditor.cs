using System.Diagnostics;
using UnityEditor;

namespace ET
{
    /// <summary>
    /// Unity 编辑器下的 CodeMode 刷新菜单入口。
    /// </summary>
    public static class CodeModeEditor
    {
        /// <summary>
        /// 菜单项 <c>ET/Init/CodeMode</c>：读取 <see cref="GlobalConfig"/> 中配置的 <see cref="GlobalConfig.CodeMode"/>，
        /// 调用 <c>Bin/ET.CodeMode.dll</c> 重新生成各包的 <c>AssemblyReference.asmref</c> 并刷新 Unity 资源数据库。
        /// </summary>
        [MenuItem("ET/Init/CodeMode")]
        public static void Init()
        {
            var globalConfig = UnityEngine.Resources.Load<GlobalConfig>("GlobalConfig");
            Process process = ProcessHelper.DotNet($"Bin/ET.CodeMode.dll --CodeMode={globalConfig.CodeMode}", ".", true);
            process.WaitForExit();
            AssetDatabase.Refresh();
        }
    }
}
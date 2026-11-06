using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// Unity 编辑器编译宏（Scripting Define Symbols）开关辅助类。
    /// </summary>
    public static class DefineHelper
    {
        /// <summary>
        /// 为当前选中的构建目标平台组启用或禁用指定的预处理宏符号。
        /// </summary>
        /// <param name="symbols">要添加或移除的编译宏名称（如 <c>ENABLE_VIEW</c>）。</param>
        /// <param name="enable">true 表示添加该宏，false 表示移除该宏。</param>
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
    }
}
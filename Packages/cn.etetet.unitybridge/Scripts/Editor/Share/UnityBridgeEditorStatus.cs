using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// UnityBridge 编辑器状态查询辅助类，通过反射读取 Resources/GlobalConfig 资产获取当前项目的 CodeMode 配置。
    /// </summary>
    internal static class UnityBridgeEditorStatus
    {
        public static string GetCodeMode()
        {
            Type globalConfigType = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(static assembly => assembly.GetType("ET.GlobalConfig"))
                    .FirstOrDefault(static type => type != null);
            if (globalConfigType == null)
            {
                return string.Empty;
            }

            UnityEngine.Object globalConfig = Resources.Load("GlobalConfig", globalConfigType);
            if (globalConfig == null)
            {
                return string.Empty;
            }

            FieldInfo codeModeField = globalConfigType.GetField("CodeMode");
            return codeModeField?.GetValue(globalConfig)?.ToString() ?? string.Empty;
        }
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditorInternal;

namespace ET
{
    /// <summary>
    /// Unity 编辑器控制台日志双击跳转重定向工具。
    /// 当双击 Unity Console 中的日志时，由于日志统一通过 <c>Log.cs</c> / <c>UnityLogger.cs</c> / <c>YooLogger.cs</c> 封装输出，
    /// 默认会跳进日志封装类内部；本类通过拦截 <see cref="OnOpenAssetAttribute"/> 并解析堆栈，自动跳过日志封装帧，直接打开真实触发日志的业务代码文件与对应行号。
    /// </summary>
    internal static class LogRedirection
    {
        /// <summary>
        /// 拦截 Unity 打开脚本资产事件（优先级 0）：若双击的是日志封装脚本，则从控制台堆栈中向下查找第一个非日志封装帧并在 IDE 中打开。
        /// </summary>
        [OnOpenAsset(0)]
        private static bool OnOpenAsset(int instanceID, int line)
        {
            if (line <= 0)
            {
                return false;
            }

            Regex logFileRegex = new(@"((Log\.cs)|(UnityLogger\.cs)|(YooLogger\.cs))");
            string codePath = AssetDatabase.GetAssetPath(instanceID);
            if (logFileRegex.IsMatch(codePath))
            {
                var content = GetStackTrace();
                // 如果有手动输入的地址，就跳过
                var hrefMath = Regex.Match(content, @"<a href=""(.*?)"" line=""(\w+)\"">.*?</a>");
                if (hrefMath.Success)
                {
                    OpenIDE(hrefMath.Groups[1].Value, int.Parse(hrefMath.Groups[2].Value));
                    return true;
                }
                
                Match stackLineMatch = Regex.Match(content, @"\(at (.+):([0-9]+)\)");
                while (stackLineMatch.Success)
                {
                    codePath = stackLineMatch.Groups[1].Value;
                    if (!logFileRegex.IsMatch(codePath))
                    {
                        int matchLine = int.Parse(stackLineMatch.Groups[2].Value);
                        OpenIDE(codePath, matchLine);
                        return true;
                    }
                    stackLineMatch = stackLineMatch.NextMatch();
                }
            }
            
            return false;
        }

        /// <summary>
        /// 调用外部代码编辑器打开指定路径的代码文件并定位到指定行列。
        /// </summary>
        private static void OpenIDE(string path, int line, int column = 0)
        {
            if (!Path.IsPathFullyQualified(path))
            {
                path = Path.GetFullPath(path);
            }
            // 跳转到目标代码的特定行
            InternalEditorUtility.OpenFileAtLineExternal(path, line, column);
        }

        /// <summary>
        /// 获取当前日志窗口选中的日志的堆栈信息。
        /// </summary>
        /// <returns>选中日志的堆栈信息实例。</returns>
        private static string GetStackTrace()
        {
            // 通过反射获取ConsoleWindow类
            var consoleWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ConsoleWindow");
            // 获取窗口实例
            var fieldInfo = consoleWindowType.GetField("ms_ConsoleWindow",
                BindingFlags.Static |
                BindingFlags.NonPublic);
            if (fieldInfo != null)
            {
                var consoleInstance = fieldInfo.GetValue(null);
                if (consoleInstance != null)
                {
                    if (EditorWindow.focusedWindow == (EditorWindow)consoleInstance)
                    {
                        // 获取m_ActiveText成员
                        fieldInfo = consoleWindowType.GetField("m_ActiveText",
                            BindingFlags.Instance |
                            BindingFlags.NonPublic);
                        // 获取m_ActiveText的值
                        if (fieldInfo != null)
                        {
                            var activeText = fieldInfo.GetValue(consoleInstance).ToString();
                            return activeText;
                        }
                    }
                }
            }

            return null;
        }
    }
}

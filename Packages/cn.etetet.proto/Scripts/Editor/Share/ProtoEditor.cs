using UnityEditor;
using System.Diagnostics;

namespace ET
{
    /// <summary>
    /// Unity 编辑器下的 Proto 协议导出菜单入口。
    /// </summary>
    public static class ProtoEditor
    {
        /// <summary>
        /// 菜单项 <c>ET/Proto/Proto2CS</c>：调用 <c>./Bin/ET.Proto2CS.dll</c> 将 <c>MainPackage.txt</c> 中各包的 <c>.proto</c> 文件导出为 C# 消息代码。
        /// </summary>
        [MenuItem("ET/Proto/Proto2CS")]
        public static void Run()
        {
            ProcessHelper.DotNet("./Bin/ET.Proto2CS.dll", "./", true);
        }
        
        /// <summary>
        /// 供外部或初始化流程调用的协议导出入口。
        /// </summary>
        public static void Init()
        {
            Run();
        }
    }
}
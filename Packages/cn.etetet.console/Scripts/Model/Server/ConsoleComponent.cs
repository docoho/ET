using System.Collections.Generic;
using System.Threading;

namespace ET.Server
{
    /// <summary>
    /// 服务端控制台内置命令模式名称常量定义（支持通过 <c>partial</c> 在其他包中扩展新命令，如机器人指令等）。
    /// </summary>
    public static partial class ConsoleMode
    {
        /// <summary>热重载 Hotfix DLL 指令（<c>"R"</c>）。</summary>
        public const string ReloadDll = "R";
        /// <summary>热重载 Luban 配置表指令（<c>"C"</c>）。</summary>
        public const string ReloadConfig = "C";
        /// <summary>查看内存状态指令（<c>"M"</c>）。</summary>
        public const string ShowMemory = "M";
        /// <summary>进入 C# 交互式脚本（REPL）模式指令。</summary>
        public const string Repl = "Repl";
        /// <summary>调试器模式指令。</summary>
        public const string Debugger = "Debugger";
        /// <summary>创建压测/测试机器人指令。</summary>
        public const string CreateRobot = "CreateRobot";
        /// <summary>机器人控制子模式指令。</summary>
        public const string Robot = "Robot";
    }

    /// <summary>
    /// 服务端命令行控制台交互组件（挂载在根 <see cref="Scene"/> 上）。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ConsoleComponent: Entity, IAwake, IDestroy
    {
        /// <summary>用于取消后台控制台读取任务的令牌源。</summary>
        public CancellationTokenSource CancellationTokenSource;

    }
}
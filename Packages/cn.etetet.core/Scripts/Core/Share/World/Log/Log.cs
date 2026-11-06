using System;
using System.Diagnostics;

namespace ET
{
    /// <summary>
    /// 全局静态日志门面类。
    /// <para>
    /// 【工作原理】
    /// 调用 <c>Log.Info</c> / <c>Log.Error</c> 时，内部通过 <see cref="GetLog"/> 自动优先使用当前线程正在运行的 <see cref="Fiber.Instance"/> 专属的 <see cref="ILog"/> 实例（若不在 Fiber 线程中，则回退使用全局 <see cref="Logger.Instance"/>），
    /// 从而在多纤程并发时自动在日志中带上当前纤程的名称与上下文！
    /// 此外，在 .NET 服务端下利用 C# 10 的 <c>DefaultInterpolatedStringHandler</c> 实现了日志级别不满足时的零字符串拼接分配优化。
    /// </para>
    /// </summary>
    public static class Log
    {
        private const int TraceLevel = 1;
        private const int DebugLevel = 2;
        private const int InfoLevel = 3;
        private const int WarningLevel = 4;

        private static ILog GetLog()
        {
            return Fiber.Instance != null? Fiber.Instance.Log : Logger.Instance.Log;
        }
        
        /// <summary>输出调试日志（仅在定义了 DEBUG 编译宏且 LogLevel &lt;= 2 时生效）</summary>
        [Conditional("DEBUG")]
        public static void Debug(string msg)
        {
            if (Options.Instance.LogLevel > DebugLevel)
            {
                return;
            }

            GetLog().Debug(msg);
        }
        
        /// <summary>输出附带完整调用堆栈的追踪日志（仅在定义了 DEBUG 编译宏且 LogLevel &lt;= 1 时生效）</summary>
        [Conditional("DEBUG")]
        public static void Trace(string msg)
        {
            if (Options.Instance.LogLevel > TraceLevel)
            {
                return;
            }
            StackTrace st = new(1, true);
            GetLog().Trace($"{msg}\n{st}");
        }

        /// <summary>输出常规运行信息日志（Release 下保留，受 LogLevel &lt;= 3 控制）</summary>
        public static void Info(string msg)
        {
            if (Options.Instance.LogLevel > InfoLevel)
            {
                return;
            }
            GetLog().Info(msg);
        }

        /// <summary>输出附带调用堆栈的重要信息日志</summary>
        public static void TraceInfo(string msg)
        {
            if (Options.Instance.LogLevel > InfoLevel)
            {
                return;
            }
            StackTrace st = new(1, true);
            GetLog().Trace($"{msg}\n{st}");
        }

        /// <summary>输出警告日志</summary>
        public static void Warning(string msg)
        {
            if (Options.Instance.LogLevel > WarningLevel)
            {
                return;
            }
            GetLog().Warning(msg);
        }

        /// <summary>输出错误日志（自动附带当前调用堆栈 StackTrace）</summary>
        public static void Error(string msg)
        {
            StackTrace st = new(1, true);
            GetLog().Error($"{msg}\n{st}");
        }

        /// <summary>输出异常错误日志</summary>
        public static void Error(Exception e)
        {
            GetLog().Error(e);
        }
        
        /// <summary>输出到标准控制台（当 Options.Console == 1 时）并同时记录到 Debug 日志</summary>
        public static void Console(string msg)
        {
            if (Options.Instance.Console == 1)
            {
                System.Console.WriteLine(msg);
            }
            GetLog().Debug(msg);
        }

#if DOTNET
        
        [Conditional("DEBUG")]
        public static void Trace(ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler message)
        {
            if (Options.Instance.LogLevel > TraceLevel)
            {
                return;
            }
            StackTrace st = new(1, true);
            GetLog().Trace($"{message.ToStringAndClear()}\n{st.ToString()}");
        }
        [Conditional("DEBUG")]
        public static void Warning(ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler message)
        {
            if (Options.Instance.LogLevel > WarningLevel)
            {
                return;
            }
            GetLog().Warning(ref message);
        }

        public static void Info(ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler message)
        {
            if (Options.Instance.LogLevel > InfoLevel)
            {
                return;
            }
            GetLog().Info(ref message);
        }
        [Conditional("DEBUG")]
        public static void Debug(ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler message)
        {
            if (Options.Instance.LogLevel > DebugLevel)
            {
                return;
            }
            GetLog().Debug(ref message);
        }

        public static void Error(ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler message)
        {
            GetLog().Error(ref message);
        }
#endif
    }
}

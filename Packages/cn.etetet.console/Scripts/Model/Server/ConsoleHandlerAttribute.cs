namespace ET.Server
{
    /// <summary>
    /// 服务端控制台命令处理器注册特性。
    /// <para>标记在实现 <see cref="IConsoleHandler"/> 的类上，指定其处理的控制台命令模式（如 <see cref="ConsoleMode.ReloadDll"/>）。</para>
    /// </summary>
    public class ConsoleHandlerAttribute: BaseAttribute
    {
        /// <summary>控制台命令模式标识字符串。</summary>
        public string Mode { get; }

        public ConsoleHandlerAttribute(string mode)
        {
            this.Mode = mode;
        }
    }
}
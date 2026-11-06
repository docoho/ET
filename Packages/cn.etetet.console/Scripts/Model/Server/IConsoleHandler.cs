namespace ET.Server
{
    /// <summary>
    /// 服务端控制台命令处理器统一接口。
    /// </summary>
    public interface IConsoleHandler
    {
        /// <summary>
        /// 异步执行控制台命令。
        /// </summary>
        /// <param name="fiber">当前控制台组件所属的 Fiber。</param>
        /// <param name="contex">当前控制台所处的子模式上下文（若处理器执行完毕后不希望保持在该子模式，可调用 <c>contex.Parent.RemoveComponent&lt;ModeContex&gt;()</c>）。</param>
        /// <param name="content">用户在控制台输入的完整命令行文本。</param>
        ETTask Run(Fiber fiber, ModeContex contex, string content);
    }
}
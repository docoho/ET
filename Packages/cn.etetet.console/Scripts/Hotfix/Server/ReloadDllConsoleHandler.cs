namespace ET.Server
{
    /// <summary>
    /// 控制台热重载代码程序集（<c>R</c>）命令处理器。
    /// <para>在服务端运行时通过控制台输入 <c>R</c> 即可触发 <see cref="CodeLoader.Reload"/> 重新加载最新的 Hotfix 程序集与各场景事件/系统。</para>
    /// </summary>
    [ConsoleHandler(ConsoleMode.ReloadDll)]
    public class ReloadDllConsoleHandler: IConsoleHandler
    {
        public async ETTask Run(Fiber fiber, ModeContex contex, string content)
        {
            await ETTask.CompletedTask;
            CodeLoader.Instance.Reload();
        }
    }
}
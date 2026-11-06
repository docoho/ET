using System;

namespace ET.Server
{
    /// <summary>
    /// 控制台热重载配置表（<c>C ConfigName</c>）命令处理器。
    /// <para>在服务端运行时通过控制台输入 <c>C UnitConfig</c> 等指令即可触发 <see cref="ConfigLoader"/> 重新加载配置表数据。</para>
    /// </summary>
    [ConsoleHandler(ConsoleMode.ReloadConfig)]
    public class ReloadConfigConsoleHandler: IConsoleHandler
    {
        public async ETTask Run(Fiber fiber, ModeContex contex, string content)
        {
            switch (content)
            {
                case ConsoleMode.ReloadConfig:
                    contex.Parent.RemoveComponent<ModeContex>();
                    Log.Console("C must have config name, like: C UnitConfig");
                    break;
                default:
                    string[] ss = content.Split(" ");
                    string configName = ss[1];
                    string category = $"{configName}Category";
                    Type type = CodeTypes.Instance.GetType($"ET.{category}");
                    if (type == null)
                    {
                        Log.Console($"reload config but not find {category}");
                        return;
                    }
                    await ConfigLoader.Instance.LoadAsync();
                    Log.Console($"reload config {configName} finish!");
                    break;
            }
            
            await ETTask.CompletedTask;
        }
    }
}
using System.Net;

namespace ET.Server
{
    /// <summary>
    /// 进程级内网通信纤程（<c>SceneType.NetInner</c>）初始化处理器。
    /// <para>每个服务端进程创建唯一的 <c>NetInner</c> 纤程，集中承载内网物理 Socket（<see cref="ProcessOuterSender"/>），使进程内的所有业务 Fiber 无需各自监听内网端口，而是通过 <c>NetInner</c> 统一完成跨进程消息收发。</para>
    /// </summary>
    [Invoke(SceneType.NetInner)]
    public class FiberInit_NetInner: AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Fiber fiber = fiberInit.Fiber;
            Scene root = fiber.Root;
            root.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            root.AddComponent<ProcessOuterSender, IPEndPoint>(root.GetSingleton<AddressSingleton>().InnerAddress);
            root.AddComponent<ProcessInnerSender>();
            root.AddComponent<ProcessFiberAddressComponent>();

            await ETTask.CompletedTask;
        }
    }
}

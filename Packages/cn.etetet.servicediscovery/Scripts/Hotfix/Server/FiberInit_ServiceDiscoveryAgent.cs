namespace ET.Server
{
    /// <summary>
    /// 进程级服务发现代理（ServiceDiscoveryAgent）Fiber 初始化处理器。
    /// 每个服务端进程固定启动一个 ServiceDiscoveryAgent Fiber，负责汇聚本进程所有业务 Scene 的服务注册、订阅与心跳，并与主 ServiceDiscovery 同步。
    /// </summary>
    [Invoke(SceneType.ServiceDiscoveryAgent)]
    public class FiberInit_ServiceDiscoveryAgent : AInvokeHandler<FiberInit, ETTask>
    {
        /// <summary>
        /// 执行 ServiceDiscoveryAgent 场景的初始化流程。
        /// </summary>
        /// <param name="fiberInit">Fiber 初始化事件参数</param>
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Fiber fiber = fiberInit.Fiber;
            Scene root = fiber.Root;
            root.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            root.AddComponent<ProcessInnerSender>();
            root.AddComponent<MessageSender>();
            // 若非本地单节点 Bootstrap 模式，则挂载 DBManagerComponent 以便从 MongoDB 读取当前活跃 Master 租约信息
            if (fiber.GetSingleton<ServiceDiscoveryBootstrapSingleton>() == null)
            {
                root.AddComponent<DBManagerComponent>();
            }

            // Agent 启动不阻塞等待向主节点注册完成：
            // 只拉起后台 bootstrap 协程，后续由协程完成 master 解析与注册。
            root.AddComponent<ServiceDiscoveryAgent>();
            // 将当前 Agent Fiber 地址登记到进程内地址表，供同进程的各业务 Scene（ServiceDiscoveryProxy）查找与通信
            root.AddComponent<ProcessFiberAddressComponent>();
            await ETTask.CompletedTask;
        }
    }
}

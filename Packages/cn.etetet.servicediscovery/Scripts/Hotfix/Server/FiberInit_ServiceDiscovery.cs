namespace ET.Server
{
    /// <summary>
    /// 服务发现中心（ServiceDiscovery）Fiber 初始化处理器。
    /// 负责挂载通信组件、定时器、协程锁、数据库连接（非单机 Bootstrap 模式）以及核心 <see cref="ServiceDiscovery"/> 组件，并完成主备选举初始化。
    /// </summary>
    [Invoke(SceneType.ServiceDiscovery)]
    public class FiberInit_ServiceDiscovery : AInvokeHandler<FiberInit, ETTask>
    {
        /// <summary>
        /// 执行 ServiceDiscovery 场景的初始化流程。
        /// </summary>
        /// <param name="fiberInit">Fiber 初始化事件参数</param>
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Fiber fiber = fiberInit.Fiber;
            Scene root = fiber.Root;
            // 挂载无序邮箱组件以接收来自各进程 Agent 的 RPC 消息
            root.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            root.AddComponent<ProcessInnerSender>();
            root.AddComponent<MessageSender>();

            // 若未配置进程内 Bootstrap 直连单例（生产环境多节点模式），则挂载数据库管理组件用于 MongoDB 主节点租约 CAS 抢占与续租
            if (fiber.GetSingleton<ServiceDiscoveryBootstrapSingleton>() == null)
            {
                root.AddComponent<DBManagerComponent>();
            }
            
            // 添加服务发现核心组件并异步初始化主备角色
            ServiceDiscovery serviceDiscovery = root.AddComponent<ServiceDiscovery>();
            await serviceDiscovery.InitializeAsync();
        }
    }
}

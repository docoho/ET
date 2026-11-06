namespace ET.Server
{
    /// <summary>
    /// 服务发现本地直连引导单例（实现 <see cref="IInheritableSingleton"/> 可跨子 Fiber 继承）。
    /// 当配置了该单例时，ServiceDiscovery 与 ServiceDiscoveryAgent 将跳过 MongoDB 选主与持久化，直接使用注入的 <see cref="MasterActorId"/> 进行单机内存模式通信（常用于单进程测试环境）。
    /// </summary>
    public class ServiceDiscoveryBootstrapSingleton :
            Singleton<ServiceDiscoveryBootstrapSingleton>,
            ISingletonAwake,
            ISingletonAwake<ActorId>,
            IInheritableSingleton
    {
        /// <summary>预设的主服务发现节点 ActorId</summary>
        public ActorId MasterActorId { get; private set; }

        public void Awake()
        {
        }

        public void Awake(ActorId masterActorId)
        {
            this.SetMasterActorId(masterActorId);
        }

        /// <summary>设置主服务发现节点的 ActorId</summary>
        public void SetMasterActorId(ActorId masterActorId)
        {
            this.MasterActorId = masterActorId;
        }
    }
}

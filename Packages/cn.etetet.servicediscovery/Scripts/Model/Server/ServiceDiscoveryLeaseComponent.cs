namespace ET.Server
{
    /// <summary>
    /// 服务发现主备租约与 Fencing Epoch 状态组件（挂载于 <see cref="ServiceDiscovery"/>）。
    /// 记录当前识别到的活跃 Master 信息、租约到期时间、续租周期以及数据库访问熔断器状态。
    /// </summary>
    [ComponentOf(typeof(ServiceDiscovery))]
    public class ServiceDiscoveryLeaseComponent : Entity, IAwake
    {
        /// <summary>当前已知的主节点场景名称</summary>
        public string CurrentMasterSceneName;

        /// <summary>当前已知的主节点 ActorId</summary>
        public ActorId CurrentMasterActorId;

        /// <summary>当前主节点的单调递增任期版本号（Fencing Epoch）</summary>
        public long CurrentMasterEpoch;

        /// <summary>当前主节点租约过期时间戳（毫秒）</summary>
        public long CurrentMasterLeaseExpireTime;

        /// <summary>当前节点是否处于活跃可写主节点状态</summary>
        public bool IsActiveMaster;

#if !UNITY_EDITOR
        /// <summary>主节点租约有效期（毫秒，非编辑器默认 5 秒）</summary>
        public long MasterLeaseTimeout = 5 * 1000;
#else
        /// <summary>编辑器模式下的主节点租约有效期（毫秒，默认 30 秒）</summary>
        public long MasterLeaseTimeout = 30 * 1000;
#endif

        /// <summary>主节点周期性续租或备节点检查租约的间隔（毫秒）</summary>
        public long MasterLeaseRenewInterval = 1 * 1000;

        /// <summary>上次执行主租约检查的单调时间戳</summary>
        public long LastMasterLeaseCheckTime;

        /// <summary>是否正在执行异步租约 Tick（防重入标志）</summary>
        public bool LeaseTickRunning;

        /// <summary>租约操作连续失败次数</summary>
        public int LeaseFailureCount;

        /// <summary>租约操作熔断器开启截止时间戳</summary>
        public long LeaseCircuitOpenUntil;

        /// <summary>下次允许重试租约操作的时间戳</summary>
        public long NextLeaseRetryTime;

        /// <summary>触发租约熔断的连续失败次数阈值</summary>
        public int LeaseCircuitThreshold = 5;

        /// <summary>租约熔断器开启持续时长（毫秒）</summary>
        public long LeaseCircuitOpenDuration = 3 * 1000;

        /// <summary>租约操作失败指数退避基础间隔（毫秒）</summary>
        public long LeaseFailureBackoffBase = 100;

        /// <summary>租约操作失败指数退避最大间隔（毫秒）</summary>
        public long LeaseFailureBackoffMax = 2 * 1000;
    }
}

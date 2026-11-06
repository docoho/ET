namespace ET
{
    /// <summary>
    /// 服务发现模块的定时器回调类型常量定义。
    /// </summary>
    public static partial class TimerInvokeType
    {
        /// <summary>业务场景 Proxy 向同进程 Agent 周期性发送心跳的定时器</summary>
        public const int ServiceDiscoveryProxyHeartbeat = PackageType.ServiceDiscovery * 1000 + 2;
        /// <summary>进程级 Agent 巡检同进程各 Proxy 心跳超时的定时器</summary>
        public const int ServiceDiscoveryAgentProxyHeartbeatCheck = PackageType.ServiceDiscovery * 1000 + 3;
    }
}

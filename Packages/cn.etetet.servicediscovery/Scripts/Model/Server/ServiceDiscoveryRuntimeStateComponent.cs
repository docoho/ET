namespace ET.Server
{
    /// <summary>
    /// 服务发现中心主节点的运行时单调时钟状态组件（挂载于 <see cref="ServiceDiscovery"/>）。
    /// 用于检测系统时钟回拨并保证内部租约与心跳时间戳单调不减。
    /// </summary>
    [ComponentOf(typeof(ServiceDiscovery))]
    public class ServiceDiscoveryRuntimeStateComponent : Entity, IAwake
    {
        /// <summary>上次采样的原始系统毫秒时间戳</summary>
        public long LastRawNow;

        /// <summary>防时钟回拨的单调递增毫秒时间戳</summary>
        public long MonotonicNow;
    }
}

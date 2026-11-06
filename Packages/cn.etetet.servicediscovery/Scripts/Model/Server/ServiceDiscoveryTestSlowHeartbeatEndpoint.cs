namespace ET.Server
{
    /// <summary>
    /// 单元测试专用的慢心跳模拟端点组件（用于测试并发心跳防重入、超时熔断与 Failover 行为）。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ServiceDiscoveryTestSlowHeartbeatEndpoint : Entity, IAwake
    {
        /// <summary>模拟处理心跳请求的延迟毫秒数</summary>
        public long HeartbeatDelayMs = 150;

        /// <summary>收到的注册请求总数</summary>
        public int RegisterRequestCount;

        /// <summary>收到的心跳请求总数</summary>
        public int HeartbeatRequestCount;

        /// <summary>当前正在并发处理中的心跳请求数</summary>
        public int CurrentHeartbeatRequests;

        /// <summary>观测到的最大并发心跳请求数峰值</summary>
        public int MaxConcurrentHeartbeatRequests;
    }
}

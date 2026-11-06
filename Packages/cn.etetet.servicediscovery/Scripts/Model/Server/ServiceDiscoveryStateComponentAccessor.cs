namespace ET.Server
{
    /// <summary>
    /// <see cref="ServiceDiscovery"/> 子状态组件的惰性获取/挂载扩展方法辅助类。
    /// </summary>
    public static class ServiceDiscoveryStateComponentAccessor
    {
        /// <summary>
        /// 获取或添加 <see cref="ServiceDiscoveryAgentHeartbeatComponent"/>。
        /// </summary>
        public static ServiceDiscoveryAgentHeartbeatComponent GetOrAddAgentHeartbeat(this ServiceDiscovery self)
        {
            ServiceDiscoveryAgentHeartbeatComponent component = self.GetComponent<ServiceDiscoveryAgentHeartbeatComponent>();
            return component ?? self.AddComponent<ServiceDiscoveryAgentHeartbeatComponent>();
        }

        /// <summary>
        /// 获取或添加 <see cref="ServiceDiscoveryLeaseComponent"/>。
        /// </summary>
        public static ServiceDiscoveryLeaseComponent GetOrAddLease(this ServiceDiscovery self)
        {
            ServiceDiscoveryLeaseComponent component = self.GetComponent<ServiceDiscoveryLeaseComponent>();
            return component ?? self.AddComponent<ServiceDiscoveryLeaseComponent>();
        }

        /// <summary>
        /// 获取或添加 <see cref="ServiceDiscoveryNotificationBufferComponent"/>。
        /// </summary>
        public static ServiceDiscoveryNotificationBufferComponent GetOrAddNotificationBuffer(this ServiceDiscovery self)
        {
            ServiceDiscoveryNotificationBufferComponent component = self.GetComponent<ServiceDiscoveryNotificationBufferComponent>();
            return component ?? self.AddComponent<ServiceDiscoveryNotificationBufferComponent>();
        }

        /// <summary>
        /// 获取或添加 <see cref="ServiceDiscoveryRuntimeStateComponent"/>。
        /// </summary>
        public static ServiceDiscoveryRuntimeStateComponent GetOrAddRuntimeState(this ServiceDiscovery self)
        {
            ServiceDiscoveryRuntimeStateComponent component = self.GetComponent<ServiceDiscoveryRuntimeStateComponent>();
            return component ?? self.AddComponent<ServiceDiscoveryRuntimeStateComponent>();
        }
    }
}

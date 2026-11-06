namespace ET.Server
{
    /// <summary>
    /// 服务发现中心主节点监控各进程 Agent 心跳超时的巡检组件系统（<see cref="ServiceDiscoveryHeartbeatChecker"/>）。
    /// 仅当所属 <see cref="ServiceDiscovery"/> 处于活跃主节点（IsActiveMaster）角色时挂载运行，按进程地址（<see cref="Address"/>）检测超时，并在超时后批量摘除该进程下的所有服务。
    /// </summary>
    [EntitySystemOf(typeof(ServiceDiscoveryHeartbeatChecker))]
    public static partial class ServiceDiscoveryHeartbeatCheckerSystem
    {
        [EntitySystem]
        private static void Awake(this ServiceDiscoveryHeartbeatChecker self)
        {
            ServiceDiscovery serviceDiscovery = self.GetParent<ServiceDiscovery>();
            self.LastCheckTime = serviceDiscovery != null ? serviceDiscovery.GetMonotonicServerNow() : self.GetSingleton<TimeInfo>().ServerNow();
        }

        [EntitySystem]
        private static void Destroy(this ServiceDiscoveryHeartbeatChecker self)
        {
        }

        /// <summary>
        /// 每帧检查是否到达 Agent 心跳巡检间隔（<see cref="ServiceDiscoveryAgentHeartbeatComponent.HeartbeatCheckInterval"/>）。
        /// </summary>
        [EntitySystem]
        private static void Update(this ServiceDiscoveryHeartbeatChecker self)
        {
            ServiceDiscovery serviceDiscovery = self.GetParent<ServiceDiscovery>();
            if (serviceDiscovery == null || !serviceDiscovery.GetOrAddLease().IsActiveMaster)
            {
                return;
            }

            long now = serviceDiscovery.GetMonotonicServerNow();
            if (now - self.LastCheckTime < serviceDiscovery.GetOrAddAgentHeartbeat().HeartbeatCheckInterval)
            {
                return;
            }

            self.LastCheckTime = now;
            self.CheckHeartbeatTimeout(serviceDiscovery);
        }

        /// <summary>
        /// 检查 Agent 心跳超时并移除同进程下所有 Proxy 服务。
        /// </summary>
        private static void CheckHeartbeatTimeout(this ServiceDiscoveryHeartbeatChecker self, ServiceDiscovery serviceDiscovery)
        {
            long now = serviceDiscovery.GetMonotonicServerNow();
            using ListComponent<Address> timeoutAddresses = ListComponent<Address>.Create();
            foreach ((Address address, long heartbeatTime) in serviceDiscovery.GetOrAddAgentHeartbeat().AgentHeartbeatTimes)
            {
                if (now - heartbeatTime > serviceDiscovery.GetOrAddAgentHeartbeat().HeartbeatTimeout)
                {
                    timeoutAddresses.Add(address);
                }
            }

            foreach (Address address in timeoutAddresses)
            {
                Log.Warning($"ServiceDiscovery agent heartbeat timeout, removing services on address: {address}");
                serviceDiscovery.GetOrAddAgentHeartbeat().AgentHeartbeatTimes.Remove(address);
                serviceDiscovery.AgentActorIds.Remove(address);
                using ListComponent<string> timeoutScenes = ListComponent<string>.Create();
                foreach ((string sceneName, EntityRef<ServiceInfo> serviceRef) in serviceDiscovery.Services)
                {
                    ServiceInfo serviceInfo = serviceRef;
                    if (serviceInfo != null && serviceInfo.ActorId.Address == address)
                    {
                        timeoutScenes.Add(sceneName);
                    }
                }

                // 逐个注销该超时进程地址下的所有服务并广播下线通知
                foreach (string sceneName in timeoutScenes)
                {
                    serviceDiscovery.UnregisterServiceAsync(sceneName, true).Coroutine();
                }
            }
        }
    }
}

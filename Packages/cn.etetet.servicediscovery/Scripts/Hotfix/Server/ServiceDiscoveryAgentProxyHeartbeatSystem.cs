using System;

namespace ET.Server
{
    /// <summary>
    /// 进程级代理监控同进程内各业务场景（<see cref="ServiceDiscoveryProxy"/>）心跳超时的组件系统（<see cref="ServiceDiscoveryAgentProxyHeartbeat"/>）。
    /// 负责周期性扫描本进程已发布服务的最后心跳时间，若某个本地业务 Scene 超时未上报心跳（例如卡死或异常未走正常注销），则自动在 Agent 侧将其注销并同步给主节点。
    /// </summary>
    [EntitySystemOf(typeof(ServiceDiscoveryAgentProxyHeartbeat))]
    public static partial class ServiceDiscoveryAgentProxyHeartbeatSystem
    {
        [EntitySystem]
        private static void Awake(this ServiceDiscoveryAgentProxyHeartbeat self)
        {
            self.StartTimer();
        }

        [EntitySystem]
        private static void Destroy(this ServiceDiscoveryAgentProxyHeartbeat self)
        {
            Scene root = self.Root();
            if (root != null)
            {
                root.TimerComponent.Remove(ref self.Timer);
            }
        }

        /// <summary>
        /// 定时检查同进程 Proxy 心跳超时的定时器回调。
        /// </summary>
        [Invoke(TimerInvokeType.ServiceDiscoveryAgentProxyHeartbeatCheck)]
        public class ServiceDiscoveryAgentProxyHeartbeatTimer : ATimer<ServiceDiscoveryAgentProxyHeartbeat>
        {
            protected override void Run(ServiceDiscoveryAgentProxyHeartbeat self)
            {
                self.CheckHeartbeatTimeoutAsync().Coroutine();
            }
        }

        /// <summary>
        /// 启动周期性心跳巡检定时器。
        /// </summary>
        private static void StartTimer(this ServiceDiscoveryAgentProxyHeartbeat self)
        {
            if (self.Timer != 0)
            {
                return;
            }

            self.Timer = self.Root().TimerComponent.NewRepeatedTimer(
                self.CheckInterval, TimerInvokeType.ServiceDiscoveryAgentProxyHeartbeatCheck, self);
        }

        /// <summary>
        /// 刷新指定本地 Proxy 场景的最后心跳时间戳，并清理超时注销重试计数。
        /// </summary>
        /// <param name="self">当前心跳监控组件</param>
        /// <param name="sceneName">业务场景名称</param>
        /// <param name="forceAdd">是否强制加入监控表（服务刚注册时为 true）</param>
        public static void TouchProxyHeartbeat(this ServiceDiscoveryAgentProxyHeartbeat self, string sceneName, bool forceAdd = false)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                return;
            }

            ServiceDiscoveryAgent agent = self.GetParent<ServiceDiscoveryAgent>();
            if (agent == null)
            {
                return;
            }

            if (!forceAdd && !self.HeartbeatTimes.ContainsKey(sceneName) && !self.IsLocalProxyScene(agent, sceneName))
            {
                return;
            }

            self.HeartbeatTimes[sceneName] = ServiceDiscoveryHelper.GetMonotonicNow(self.Root(),
                "ServiceDiscoveryAgent proxy-heartbeat", ref self.LastRawNow, ref self.MonotonicNow);
            self.TimeoutUnregisterFailureCounts.Remove(sceneName);
            self.TimeoutUnregisterNextRetryTimes.Remove(sceneName);
        }

        /// <summary>
        /// 移除指定 Proxy 场景的心跳监控记录。
        /// </summary>
        public static void RemoveProxyHeartbeat(this ServiceDiscoveryAgentProxyHeartbeat self, string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                return;
            }

            self.HeartbeatTimes.Remove(sceneName);
            self.TimeoutUnregisterFailureCounts.Remove(sceneName);
            self.TimeoutUnregisterNextRetryTimes.Remove(sceneName);
        }

        /// <summary>
        /// 执行一次同进程 Proxy 心跳超时检查（防重入）。
        /// </summary>
        private static async ETTask CheckHeartbeatTimeoutAsync(this ServiceDiscoveryAgentProxyHeartbeat self)
        {
            ServiceDiscoveryAgent agent = self.GetParent<ServiceDiscoveryAgent>();
            if (agent == null || self.Checking)
            {
                return;
            }

            self.Checking = true;
            EntityRef<ServiceDiscoveryAgentProxyHeartbeat> selfRef = self;
            EntityRef<ServiceDiscoveryAgent> agentRef = agent;
            try
            {
                self.EnsureTrackedLocalProxyScenes(agent);
                await self.CheckProxyHeartbeatTimeoutAsync(agent);
            }
            finally
            {
                self = selfRef;
                if (self != null)
                {
                    self.Checking = false;
                }
            }
        }

        /// <summary>
        /// 确保所有属于本进程地址（InnerAddress）的已缓存服务都被纳入心跳监控字典。
        /// </summary>
        private static void EnsureTrackedLocalProxyScenes(this ServiceDiscoveryAgentProxyHeartbeat self, ServiceDiscoveryAgent agent)
        {
            long now = ServiceDiscoveryHelper.GetMonotonicNow(self.Root(), "ServiceDiscoveryAgent proxy-heartbeat",
                ref self.LastRawNow, ref self.MonotonicNow);
            Address innerAddress = self.GetSingleton<AddressSingleton>().InnerAddress;
            foreach ((string sceneName, EntityRef<ServiceInfo> serviceRef) in agent.SceneNameServices)
            {
                ServiceInfo serviceInfo = serviceRef;
                if (serviceInfo == null || serviceInfo.ActorId == default)
                {
                    continue;
                }

                if (serviceInfo.ActorId.Address != innerAddress)
                {
                    continue;
                }

                if (!self.HeartbeatTimes.ContainsKey(sceneName))
                {
                    self.HeartbeatTimes[sceneName] = now;
                }
            }
        }

        /// <summary>
        /// 判断指定场景是否为本进程已发布的本地服务。
        /// </summary>
        private static bool IsLocalProxyScene(this ServiceDiscoveryAgentProxyHeartbeat self, ServiceDiscoveryAgent agent, string sceneName)
        {
            if (!agent.LocalPublishedServices.TryGetValue(sceneName, out (ActorId ActorId, StringKV Metadata) localService))
            {
                return false;
            }

            Address innerAddress = self.GetSingleton<AddressSingleton>().InnerAddress;
            return localService.ActorId != default && localService.ActorId.Address == innerAddress;
        }

        /// <summary>
        /// 扫描心跳字典，清理已不存在的陈旧场景，并对心跳超时的本地场景执行自动注销。
        /// </summary>
        private static async ETTask CheckProxyHeartbeatTimeoutAsync(this ServiceDiscoveryAgentProxyHeartbeat self, ServiceDiscoveryAgent agent)
        {
            EntityRef<ServiceDiscoveryAgentProxyHeartbeat> selfRef = self;
            EntityRef<ServiceDiscoveryAgent> agentRef = agent;
            long now = ServiceDiscoveryHelper.GetMonotonicNow(self.Root(), "ServiceDiscoveryAgent proxy-heartbeat",
                ref self.LastRawNow, ref self.MonotonicNow);
            using ListComponent<string> staleScenes = ListComponent<string>.Create();
            using ListComponent<string> timeoutScenes = ListComponent<string>.Create();
            foreach ((string sceneName, long lastHeartbeatTime) in self.HeartbeatTimes)
            {
                if (!self.IsLocalProxyScene(agent, sceneName))
                {
                    staleScenes.Add(sceneName);
                    continue;
                }

                if (now - lastHeartbeatTime > self.Timeout && self.CanRetryTimeoutUnregister(sceneName, now))
                {
                    timeoutScenes.Add(sceneName);
                }
            }

            foreach (string sceneName in staleScenes)
            {
                self.RemoveProxyHeartbeat(sceneName);
                agent.UnsubscribeProxyServiceChange(sceneName);
            }

            foreach (string sceneName in timeoutScenes)
            {
                self = selfRef;
                agent = agentRef;
                if (self == null || agent == null)
                {
                    return;
                }

                if (!self.IsLocalProxyScene(agent, sceneName))
                {
                    self.RemoveProxyHeartbeat(sceneName);
                    continue;
                }

                try
                {
                    agent.RemoveLocalServiceAfterUnregister(sceneName);
                    agent.UnsubscribeProxyServiceChange(sceneName);
                    self.RemoveProxyHeartbeat(sceneName);
                    self.OnTimeoutUnregisterCompleted(sceneName, true, ServiceDiscoveryHelper.GetMonotonicNow(self.Root(),
                        "ServiceDiscoveryAgent proxy-heartbeat", ref self.LastRawNow, ref self.MonotonicNow));
                    Log.Warning(
                        $"ServiceDiscoveryAgent proxy heartbeat timeout, unregister scene: {sceneName} agent: {agent.Root().Name}");

                    await ETTask.CompletedTask;
                }
                catch (Exception e)
                {
                    self = selfRef;
                    if (self != null)
                    {
                        self.OnTimeoutUnregisterCompleted(sceneName, false, ServiceDiscoveryHelper.GetMonotonicNow(self.Root(),
                            "ServiceDiscoveryAgent proxy-heartbeat", ref self.LastRawNow, ref self.MonotonicNow));
                    }

                    agent = agentRef;
                    if (agent != null)
                    {
                        Log.Warning(
                            $"ServiceDiscoveryAgent proxy timeout unregister failed scene: {sceneName} agent: {agent.Root().Name} error: {e.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 判断超时注销失败的场景是否已到达下次退避重试时间。
        /// </summary>
        private static bool CanRetryTimeoutUnregister(this ServiceDiscoveryAgentProxyHeartbeat self, string sceneName, long now)
        {
            return !self.TimeoutUnregisterNextRetryTimes.TryGetValue(sceneName, out long nextRetryTime) || now >= nextRetryTime;
        }

        /// <summary>
        /// 更新超时注销执行后的退避重试状态。
        /// </summary>
        private static void OnTimeoutUnregisterCompleted(this ServiceDiscoveryAgentProxyHeartbeat self, string sceneName, bool success, long now)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                return;
            }

            if (success)
            {
                self.TimeoutUnregisterFailureCounts.Remove(sceneName);
                self.TimeoutUnregisterNextRetryTimes.Remove(sceneName);
                return;
            }

            int failureCount = 1;
            if (self.TimeoutUnregisterFailureCounts.TryGetValue(sceneName, out int count))
            {
                failureCount = count + 1;
            }

            self.TimeoutUnregisterFailureCounts[sceneName] = failureCount;
            long backoff = ServiceDiscoveryHelper.GetRetryBackoff(failureCount, self.TimeoutUnregisterBackoffBase,
                self.TimeoutUnregisterBackoffMax);
            self.TimeoutUnregisterNextRetryTimes[sceneName] = now + backoff;
        }

    }
}

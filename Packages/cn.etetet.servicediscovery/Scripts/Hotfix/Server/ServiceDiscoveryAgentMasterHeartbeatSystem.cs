using System;

namespace ET.Server
{
    /// <summary>
    /// 进程级代理向主服务发现节点发送心跳的组件系统（<see cref="ServiceDiscoveryAgentMasterHeartbeat"/>）。
    /// 负责周期性向活跃 Master 发送进程级聚合心跳（<see cref="ServiceHeartbeatRequest"/>），并在遇到主节点不可用或超时异常时触发故障切换（Failover）与后台重注册。
    /// </summary>
    [EntitySystemOf(typeof(ServiceDiscoveryAgentMasterHeartbeat))]
    public static partial class ServiceDiscoveryAgentMasterHeartbeatSystem
    {
        /// <summary>
        /// 组件唤醒时启动后台心跳协程循环。
        /// </summary>
        [EntitySystem]
        private static void Awake(this ServiceDiscoveryAgentMasterHeartbeat self)
        {
            self.HeartbeatLoopAsync().Coroutine();
        }

        [EntitySystem]
        private static void Destroy(this ServiceDiscoveryAgentMasterHeartbeat self)
        {
        }

        /// <summary>
        /// 持续按 <see cref="ServiceDiscoveryAgentMasterHeartbeat.Interval"/> 间隔向主节点发送心跳的协程循环。
        /// </summary>
        private static async ETTask HeartbeatLoopAsync(this ServiceDiscoveryAgentMasterHeartbeat self)
        {
            EntityRef<ServiceDiscoveryAgentMasterHeartbeat> selfRef = self;
            ServiceDiscoveryAgent agent = self.GetParent<ServiceDiscoveryAgent>();
            EntityRef<ServiceDiscoveryAgent> agentRef = agent;

            while (true)
            {
                self = selfRef;
                if (self == null)
                {
                    return;
                }

                long interval = self.Interval > 0 ? self.Interval : 1;
                await self.Root().TimerComponent.WaitAsync(interval);

                self = selfRef;
                agent = agentRef;
                if (self == null || agent == null)
                {
                    return;
                }

                await self.SendHeartbeatToDiscoveryAsync();
            }
        }

        /// <summary>
        /// 单次向主节点发送心跳请求，支持熔断器（Circuit Breaker）与退避重试状态管理。
        /// </summary>
        private static async ETTask SendHeartbeatToDiscoveryAsync(this ServiceDiscoveryAgentMasterHeartbeat self)
        {
            ServiceDiscoveryAgent agent = self.GetParent<ServiceDiscoveryAgent>();
            if (agent == null)
            {
                return;
            }

            // 获取单调递增时间戳，若处于熔断打开期或未到达下次退避重试时间则跳过本次发送
            long now = ServiceDiscoveryHelper.GetMonotonicNow(self.Root(), "ServiceDiscoveryAgent heartbeat",
                ref self.LastRawNow, ref self.MonotonicNow);
            if (self.CircuitOpenUntil > now || self.NextRetryTime > now)
            {
                return;
            }

            EntityRef<ServiceDiscoveryAgentMasterHeartbeat> selfRef = self;
            EntityRef<ServiceDiscoveryAgent> agentRef = agent;
            bool success = false;
            bool shouldRecordResult = false;
            try
            {
                self = selfRef;
                agent = agentRef;
                if (self == null || agent == null || !agent.IsReady())
                {
                    return;
                }

                Scene agentRoot = agent.Root();
                ActorId agentActorId = agentRoot.GetActorId();
                ActorId targetActorId = agent.ServiceDiscoveryActorId;
                string sceneName = agentRoot.Name;
                long rpcTimeout = self.RpcTimeout;
                shouldRecordResult = true;
                try
                {
                    using ServiceHeartbeatRequest request = ServiceHeartbeatRequest.Create();
                    request.AgentActorId = agentActorId;
                    using ServiceHeartbeatResponse _ = await agent.MessageSender.Call(targetActorId, request, rpcTimeout)
                        as ServiceHeartbeatResponse;
                    success = true;
                }
                catch (Exception e)
                {
                    agent = agentRef;
                    if (agent != null)
                    {
                        // 如果异常表明当前主节点已失联或已降级为 Follower，则使当前端点失效并拉起后台重新解析与注册
                        if (ShouldMarkMasterUnavailable(e))
                        {
                            agent.InvalidateEndpointAndTriggerBackgroundRegister("heartbeat-failure");
                        }

                        Log.Warning(
                            $"ServiceDiscoveryAgent heartbeat failed agent: {agentActorId} scene: {sceneName} error: {e.Message}");
                    }
                }
            }
            finally
            {
                self = selfRef;
                if (self != null)
                {
                    if (shouldRecordResult)
                    {
                        self.OnRpcCompleted(success, "agent-master-heartbeat");
                    }
                }
            }
        }

        /// <summary>
        /// 记录心跳 RPC 执行结果：成功时重置退避与熔断状态，失败时累加失败次数并计算指数退避或打开熔断器。
        /// </summary>
        private static void OnRpcCompleted(this ServiceDiscoveryAgentMasterHeartbeat self, bool success, string traceName)
        {
            if (success)
            {
                ServiceDiscoveryHelper.ResetRetryState(ref self.FailureCount, ref self.CircuitOpenUntil, ref self.NextRetryTime);
                return;
            }

            long now = ServiceDiscoveryHelper.GetMonotonicNow(self.Root(), "ServiceDiscoveryAgent heartbeat",
                ref self.LastRawNow, ref self.MonotonicNow);
            bool circuitOpened = ServiceDiscoveryHelper.RecordRetryFailure(ref self.FailureCount, ref self.CircuitOpenUntil,
                ref self.NextRetryTime, self.CircuitThreshold, self.CircuitOpenDuration, self.RetryBackoffBase,
                self.RetryBackoffMax, now);
            if (circuitOpened)
            {
                Log.Warning(
                    $"ServiceDiscovery heartbeat circuit opened trace: {traceName} failureCount: {self.FailureCount} openUntil: {self.CircuitOpenUntil}");
            }
        }

        /// <summary>
        /// 判断心跳异常是否应当触发主节点失效重连。
        /// </summary>
        private static bool ShouldMarkMasterUnavailable(Exception e)
        {
            if (e is not RpcException rpcException)
            {
                return true;
            }

            return rpcException.Error == ErrorCode.ERR_Cancel
                   || rpcException.Error == ErrorCode.ERR_Timeout
                   || ServiceDiscoveryErrorHelper.ShouldTriggerFailover(rpcException.Error);
        }
    }
}

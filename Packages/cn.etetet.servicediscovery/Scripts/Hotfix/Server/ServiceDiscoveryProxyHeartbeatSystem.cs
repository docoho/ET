using System;

namespace ET.Server
{
    /// <summary>
    /// 业务场景代理向同进程 Agent 发送心跳的组件系统（<see cref="ServiceDiscoveryProxyHeartbeat"/>）。
    /// 负责周期性通过进程内消息向 <see cref="ServiceDiscoveryAgent"/> 发送 <see cref="ServiceHeartbeatRequest"/>，报告当前业务 Scene 存活。
    /// </summary>
    [EntitySystemOf(typeof(ServiceDiscoveryProxyHeartbeat))]
    public static partial class ServiceDiscoveryProxyHeartbeatSystem
    {
        [EntitySystem]
        private static void Awake(this ServiceDiscoveryProxyHeartbeat self)
        {
            self.StartTimer();
        }

        [EntitySystem]
        private static void Destroy(this ServiceDiscoveryProxyHeartbeat self)
        {
            Scene root = self.Root();
            if (root != null)
            {
                root.TimerComponent.Remove(ref self.Timer);
            }
        }

        /// <summary>
        /// 业务场景 Proxy 周期性向同进程 Agent 发送心跳的定时器处理器。
        /// </summary>
        [Invoke(TimerInvokeType.ServiceDiscoveryProxyHeartbeat)]
        public class ServiceDiscoveryProxyHeartbeatTimer : ATimer<ServiceDiscoveryProxyHeartbeat>
        {
            protected override void Run(ServiceDiscoveryProxyHeartbeat self)
            {
                self.SendHeartbeatToAgentAsync().Coroutine();
            }
        }

        /// <summary>
        /// 启动周期性心跳定时器。
        /// </summary>
        private static void StartTimer(this ServiceDiscoveryProxyHeartbeat self)
        {
            if (self.Timer != 0)
            {
                return;
            }

            self.Timer = self.Root().TimerComponent.NewRepeatedTimer(
                self.Interval, TimerInvokeType.ServiceDiscoveryProxyHeartbeat, self);
        }

        /// <summary>
        /// 单次向同进程 Agent 发送心跳请求（含防重入与熔断退避机制）。
        /// </summary>
        private static async ETTask SendHeartbeatToAgentAsync(this ServiceDiscoveryProxyHeartbeat self)
        {
            ServiceDiscoveryProxy proxy = self.GetParent<ServiceDiscoveryProxy>();
            if (proxy == null || proxy.AgentFiberInstanceId == default || self.Sending)
            {
                return;
            }

            long now = ServiceDiscoveryHelper.GetMonotonicNow(self.Root(), "ServiceDiscoveryProxy heartbeat",
                ref self.LastRawNow, ref self.MonotonicNow);
            if (self.CircuitOpenUntil > now || self.NextRetryTime > now)
            {
                return;
            }

            self.Sending = true;
            EntityRef<ServiceDiscoveryProxyHeartbeat> selfRef = self;
            EntityRef<ServiceDiscoveryProxy> proxyRef = proxy;
            bool success = false;
            try
            {
                ServiceHeartbeatRequest request = ServiceHeartbeatRequest.Create();
                request.SceneName = proxy.Root().Name;
                using ServiceHeartbeatResponse _ =
                    await proxy.ForwardToDiscoveryByFailover<ServiceHeartbeatResponse>(request, nameof(ServiceHeartbeatRequest));
                success = true;
            }
            catch (RpcException rpcException)
            {
                proxy = proxyRef;
                if (proxy != null)
                {
                    Log.Warning(
                        $"ServiceDiscovery proxy heartbeat failed scene: {proxy.Root().Name} target: {proxy.AgentFiberInstanceId} error: {rpcException.Error} message: {rpcException.Message}");
                }
            }
            catch (Exception e)
            {
                proxy = proxyRef;
                if (proxy != null)
                {
                    Log.Warning(
                        $"ServiceDiscovery proxy heartbeat failed scene: {proxy.Root().Name} target: {proxy.AgentFiberInstanceId} error: {e.Message}");
                }
            }
            finally
            {
                self = selfRef;
                if (self != null)
                {
                    self.Sending = false;
                    self.OnRpcCompleted(success, "proxy-agent-heartbeat");
                }
            }
        }

        /// <summary>
        /// 记录心跳结果并维护熔断器与退避重试状态。
        /// </summary>
        private static void OnRpcCompleted(this ServiceDiscoveryProxyHeartbeat self, bool success, string traceName)
        {
            if (success)
            {
                ServiceDiscoveryHelper.ResetRetryState(ref self.FailureCount, ref self.CircuitOpenUntil, ref self.NextRetryTime);
                return;
            }

            long now = ServiceDiscoveryHelper.GetMonotonicNow(self.Root(), "ServiceDiscoveryProxy heartbeat",
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
    }
}

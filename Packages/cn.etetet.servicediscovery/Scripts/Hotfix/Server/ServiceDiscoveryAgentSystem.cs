using System;
using System.Collections.Generic;
using System.Linq;

namespace ET.Server
{
    /// <summary>
    /// 进程级服务发现代理组件系统（<see cref="ServiceDiscoveryAgent"/>）。
    /// 架构定位：
    /// 1. 位于业务场景（<see cref="ServiceDiscoveryProxy"/>）与服务发现中心（<see cref="ServiceDiscovery"/>）之间的中间聚合层；
    /// 2. 负责解析活跃 Master 节点（支持本地 Bootstrap 单例直连或从 MongoDB 读取租约记录），并在后台执行全量 Agent 注册与增量服务增删同步；
    /// 3. 维护进程级全量服务本地缓存与二级索引，向本进程内订阅了服务变更的业务场景（Proxy）扇出通知，并执行 Fencing Epoch 校验以隔离旧主脏推送。
    /// </summary>
    [EntitySystemOf(typeof(ServiceDiscoveryAgent))]
    public static partial class ServiceDiscoveryAgentSystem
    {
        /// <summary>主节点查找退避重试最大阶数</summary>
        private const int MasterLookupMaxRetryCount = 30;
        /// <summary>主节点解析重试基础退避间隔（毫秒）</summary>
        private const int MasterResolveRetryBaseInterval = 100;
        /// <summary>主节点解析重试最大退避间隔（毫秒）</summary>
        private const int MasterResolveRetryMaxInterval = 1000;
        /// <summary>当主节点不可用时，外部请求等待 Agent 就绪的最大重试次数</summary>
        private const int MasterResolveMaxRetryCountWhenEndpointUnavailable = 30;
        /// <summary>向主节点注册 Agent 的超时时间（毫秒）</summary>
        private const int RegisterAgentTimeoutMs = 5000;

        /// <summary>
        /// 组件唤醒时获取跨进程消息发送器、挂载本地 Proxy 心跳巡检组件，并拉起后台主节点解析与全量注册循环。
        /// </summary>
        [EntitySystem]
        private static void Awake(this ServiceDiscoveryAgent self)
        {
            Scene root = self.Root();
            self.MessageSender = root.GetComponent<MessageSender>();

            self.AddComponent<ServiceDiscoveryAgentProxyHeartbeat>();
            self.EnsureBackgroundRegisterStarted();
        }

        [EntitySystem]
        private static void Destroy(this ServiceDiscoveryAgent self)
        {
        }

        /// <summary>
        /// 判断当前 Agent 是否已完成向活跃 Master 节点的全量注册并处于就绪状态。
        /// </summary>
        public static bool IsReady(this ServiceDiscoveryAgent self)
        {
            return self != null
                   && self.ServiceDiscoveryActorId != default
                   && self.HasStatus(ServiceDiscoveryAgentStatus.AgentRegistered);
        }

        /// <summary>
        /// 检查当前 Agent 是否包含指定的状态标志位。
        /// </summary>
        private static bool HasStatus(this ServiceDiscoveryAgent self, ServiceDiscoveryAgentStatus status)
        {
            return (self.Status & status) != 0;
        }

        /// <summary>
        /// 设置指定的状态标志位。
        /// </summary>
        private static void SetStatus(this ServiceDiscoveryAgent self, ServiceDiscoveryAgentStatus status)
        {
            self.Status |= status;
        }

        /// <summary>
        /// 清除指定的状态标志位。
        /// </summary>
        private static void ClearStatus(this ServiceDiscoveryAgent self, ServiceDiscoveryAgentStatus status)
        {
            self.Status &= ~status;
        }

        /// <summary>
        /// 确保已挂载向主节点发送心跳的组件（<see cref="ServiceDiscoveryAgentMasterHeartbeat"/>）。
        /// </summary>
        public static void EnsureMasterHeartbeatComponent(this ServiceDiscoveryAgent self)
        {
            if (self == null)
            {
                return;
            }

            if (self.GetComponent<ServiceDiscoveryAgentMasterHeartbeat>() == null)
            {
                self.AddComponent<ServiceDiscoveryAgentMasterHeartbeat>();
            }
        }

        /// <summary>
        /// 移除向主节点发送心跳的组件（当主节点切换或失效时调用）。
        /// </summary>
        public static void RemoveMasterHeartbeatComponent(this ServiceDiscoveryAgent self)
        {
            if (self == null)
            {
                return;
            }

            if (self.GetComponent<ServiceDiscoveryAgentMasterHeartbeat>() != null)
            {
                self.RemoveComponent<ServiceDiscoveryAgentMasterHeartbeat>();
            }
        }

        /// <summary>
        /// 触发后台主节点解析与全量注册协程。
        /// </summary>
        public static void TriggerBackgroundRegister(this ServiceDiscoveryAgent self)
        {
            self.EnsureBackgroundRegisterStarted();
        }

        /// <summary>
        /// 触发后台增量服务变更（Dirty Scenes）同步协程。
        /// </summary>
        public static void TriggerBackgroundMutationSync(this ServiceDiscoveryAgent self)
        {
            self.EnsureBackgroundMutationSyncStarted();
        }

        /// <summary>
        /// 若当前尚未处于 Bootstrapping 状态，则标记状态并启动后台全量注册协程。
        /// </summary>
        private static void EnsureBackgroundRegisterStarted(this ServiceDiscoveryAgent self)
        {
            if (self == null || self.HasStatus(ServiceDiscoveryAgentStatus.Bootstrapping))
            {
                return;
            }

            self.SetStatus(ServiceDiscoveryAgentStatus.Bootstrapping);
            self.BackgroundRegisterLoopAsync().Coroutine();
        }

        /// <summary>
        /// 若 Agent 已就绪且存在未同步的本地服务变更，且尚未启动增量同步协程，则启动后台增量同步协程。
        /// </summary>
        private static void EnsureBackgroundMutationSyncStarted(this ServiceDiscoveryAgent self)
        {
            if (self == null || !self.IsReady() || !self.HasDirtyPublishedScenes() ||
                self.HasStatus(ServiceDiscoveryAgentStatus.MutationSyncing))
            {
                return;
            }

            self.SetStatus(ServiceDiscoveryAgentStatus.MutationSyncing);
            self.BackgroundMutationSyncLoopAsync().Coroutine();
        }

        /// <summary>
        /// 后台全量注册协程循环：不断尝试解析活跃主节点并携带本进程已发布服务列表向 Master 发起全量注册，直到成功或组件销毁。
        /// </summary>
        private static async ETTask BackgroundRegisterLoopAsync(this ServiceDiscoveryAgent self)
        {
            EntityRef<ServiceDiscoveryAgent> selfRef = self;
            int retry = 0;
            try
            {
                while (true)
                {
                    self = selfRef;
                    if (self == null)
                    {
                        return;
                    }

                    int delay;
                    try
                    {
                        bool ready = await self.TryEnsureAgentRegisteredOnceAsync();
                        self = selfRef;
                        if (self == null)
                        {
                            return;
                        }

                        if (ready)
                        {
                            return;
                        }

                        delay = GetMasterResolveRetryDelay(retry);
                        if (retry < MasterLookupMaxRetryCount)
                        {
                            ++retry;
                        }
                    }
                    catch (Exception e)
                    {
                        self = selfRef;
                        if (self == null)
                        {
                            return;
                        }

                        delay = GetMasterResolveRetryDelay(retry);
                        if (retry < MasterLookupMaxRetryCount)
                        {
                            ++retry;
                        }

                        Log.Error($"ServiceDiscoveryAgent background register loop failed scene: {self.Root().Name} error: {e}");
                    }

                    bool waitSucceeded = await WaitBackgroundRegisterRetryDelayAsync(selfRef, delay);
                    if (!waitSucceeded)
                    {
                        return;
                    }
                }
            }
            finally
            {
                self = selfRef;
                if (self != null)
                {
                    self.ClearStatus(ServiceDiscoveryAgentStatus.Bootstrapping);
                }
            }
        }

        /// <summary>
        /// 后台增量变更同步协程循环：将 <see cref="ServiceDiscoveryAgent.DirtyPublishedScenes"/> 中记录的本地服务增删同步给主节点。
        /// </summary>
        private static async ETTask BackgroundMutationSyncLoopAsync(this ServiceDiscoveryAgent self)
        {
            EntityRef<ServiceDiscoveryAgent> selfRef = self;
            int retry = 0;
            try
            {
                while (true)
                {
                    self = selfRef;
                    if (self == null)
                    {
                        return;
                    }

                    if (!self.IsReady() || !self.HasDirtyPublishedScenes())
                    {
                        return;
                    }

                    int delay;
                    try
                    {
                        bool drained = await self.TryFlushDirtyScenesOnceAsync();
                        self = selfRef;
                        if (self == null)
                        {
                            return;
                        }

                        if (drained || !self.IsReady() || !self.HasDirtyPublishedScenes())
                        {
                            return;
                        }

                        delay = 0;
                        retry = 0;
                    }
                    catch (Exception e)
                    {
                        self = selfRef;
                        if (self == null)
                        {
                            return;
                        }

                        delay = GetMasterResolveRetryDelay(retry);
                        if (retry < MasterLookupMaxRetryCount)
                        {
                            ++retry;
                        }

                        Log.Error($"ServiceDiscoveryAgent background mutation sync loop failed scene: {self.Root().Name} error: {e}");
                    }

                    bool waitSucceeded = await WaitBackgroundMutationSyncRetryDelayAsync(selfRef, delay);
                    if (!waitSucceeded)
                    {
                        return;
                    }
                }
            }
            finally
            {
                self = selfRef;
                if (self != null)
                {
                    self.ClearStatus(ServiceDiscoveryAgentStatus.MutationSyncing);
                    if (self.IsReady() && self.HasDirtyPublishedScenes())
                    {
                        self.TriggerBackgroundMutationSync();
                    }
                }
            }
        }

        /// <summary>
        /// 等待后台注册重试退避延迟。
        /// </summary>
        private static async ETTask<bool> WaitBackgroundRegisterRetryDelayAsync(EntityRef<ServiceDiscoveryAgent> selfRef, int delay)
        {
            ServiceDiscoveryAgent self = selfRef;
            if (self == null)
            {
                return false;
            }

            if (delay <= 0)
            {
                return true;
            }

            try
            {
                await self.Root().TimerComponent.WaitAsync(delay);
                return true;
            }
            catch (Exception e)
            {
                self = selfRef;
                if (self == null)
                {
                    return false;
                }

                Log.Warning(
                    $"ServiceDiscoveryAgent background register retry wait aborted scene: {self.Root().Name} delay: {delay} error: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 等待后台增量同步重试退避延迟。
        /// </summary>
        private static async ETTask<bool> WaitBackgroundMutationSyncRetryDelayAsync(EntityRef<ServiceDiscoveryAgent> selfRef, int delay)
        {
            ServiceDiscoveryAgent self = selfRef;
            if (self == null)
            {
                return false;
            }

            if (delay <= 0)
            {
                return true;
            }

            try
            {
                await self.Root().TimerComponent.WaitAsync(delay);
                return true;
            }
            catch (Exception e)
            {
                self = selfRef;
                if (self == null)
                {
                    return false;
                }

                Log.Warning(
                    $"ServiceDiscoveryAgent background mutation retry wait aborted scene: {self.Root().Name} delay: {delay} error: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 判断当前是否需要向主节点执行全量注册。
        /// </summary>
        private static bool NeedRegisterToDiscovery(this ServiceDiscoveryAgent self)
        {
            return self != null
                   && (self.ServiceDiscoveryActorId == default
                       || !self.HasStatus(ServiceDiscoveryAgentStatus.AgentRegistered));
        }

        /// <summary>
        /// 判断是否存在尚未同步到主节点的本地服务变更。
        /// </summary>
        private static bool HasDirtyPublishedScenes(this ServiceDiscoveryAgent self)
        {
            return self != null && self.DirtyPublishedScenes.Count > 0;
        }

        /// <summary>
        /// 在协程锁保护下尝试执行一次主节点地址解析与全量 Agent 注册。
        /// </summary>
        private static async ETTask<bool> TryEnsureAgentRegisteredOnceAsync(this ServiceDiscoveryAgent self)
        {
            EntityRef<ServiceDiscoveryAgent> selfRef = self;
            using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.ServiceDiscoveryAgentInit, self.Id))
            {
                self = selfRef;
                if (self == null)
                {
                    return false;
                }

                if (!self.NeedRegisterToDiscovery())
                {
                    return true;
                }

                if (self.ServiceDiscoveryActorId == default)
                {
                    // 优先尝试本地进程内 Bootstrap 注入的主节点端点；若不存在则从 MongoDB 加载当前活跃 Master 记录
                    if (!self.TryApplyInheritedMasterEndpoint())
                    {
                        await self.TryLoadActiveMasterFromDbAsync();
                        self = selfRef;
                        if (self == null)
                        {
                            return false;
                        }

                        if (self.ServiceDiscoveryActorId == default)
                        {
                            return false;
                        }
                    }
                }

                Dictionary<string, int> versionSnapshot = self.CapturePublishedSceneVersions();
                bool registered = await self.TryRegisterAgentToDiscoveryAsync(versionSnapshot);
                self = selfRef;
                if (self == null)
                {
                    return false;
                }

                if (!registered)
                {
                    return false;
                }

                // 若在全量注册期间又有新的本地服务变更产生，则立即触发增量同步
                if (self.IsReady() && self.HasDirtyPublishedScenes())
                {
                    self.TriggerBackgroundMutationSync();
                }

                return self.IsReady();
            }
        }

        /// <summary>
        /// 尝试从 <see cref="ServiceDiscoveryBootstrapSingleton"/> 获取本地单机直连的主节点 ActorId。
        /// </summary>
        private static bool TryApplyInheritedMasterEndpoint(this ServiceDiscoveryAgent self)
        {
            ServiceDiscoveryBootstrapSingleton bootstrap =
                    self.Fiber().GetSingleton<ServiceDiscoveryBootstrapSingleton>();
            if (bootstrap == null || bootstrap.MasterActorId == default)
            {
                return false;
            }

            if (self.CurrentMasterEpoch <= 0)
            {
                self.CurrentMasterEpoch = 1;
            }

            self.SwitchToEndpoint(bootstrap.MasterActorId, "inheritable-endpoint");
            return true;
        }

        /// <summary>
        /// 等待当前 Agent 完成主节点注册并进入就绪状态；若超过最大重试次数仍未就绪，则抛出 <see cref="ErrorCode.ERR_ServiceDiscoveryMasterUnavailable"/>。
        /// </summary>
        private static async ETTask EnsureReadyForRequestAsync(this ServiceDiscoveryAgent self, string requestName)
        {
            EntityRef<ServiceDiscoveryAgent> selfRef = self;
            self = selfRef;
            if (self == null)
            {
                throw new Exception("service discovery agent disposed");
            }

            if (self.IsReady())
            {
                return;
            }

            int totalRetryCount = MasterResolveMaxRetryCountWhenEndpointUnavailable;
            for (int retry = 0; retry < totalRetryCount; ++retry)
            {
                self = selfRef;
                if (self == null)
                {
                    throw new Exception("service discovery agent disposed");
                }

                if (!self.HasStatus(ServiceDiscoveryAgentStatus.Bootstrapping))
                {
                    self.TriggerBackgroundRegister();
                }

                if (self.IsReady())
                {
                    return;
                }

                if (retry + 1 < totalRetryCount)
                {
                    int delay = GetMasterResolveRetryDelay(retry);
                    await self.Root().TimerComponent.WaitAsync(delay);
                }
            }

            self = selfRef;
            if (self == null)
            {
                throw new Exception("service discovery agent disposed");
            }

            throw new RpcException(ErrorCode.ERR_ServiceDiscoveryMasterUnavailable,
                $"service discovery agent not ready, request: {requestName}, scene: {self.Root().Name}");
        }

        /// <summary>
        /// 从 MongoDB 查询主节点租约记录（<see cref="ServiceDiscoveryMaster"/>），校验租约未过期且 Epoch 单调不减后切换到该主节点端点。
        /// </summary>
        private static async ETTask TryLoadActiveMasterFromDbAsync(this ServiceDiscoveryAgent self)
        {
            if (self == null)
            {
                return;
            }

            EntityRef<ServiceDiscoveryAgent> selfRef = self;
            Scene root = self.Root();
            string rootName = root.Name;
            DBManagerComponent dbManagerComponent = root.GetComponent<DBManagerComponent>();
            if (dbManagerComponent == null)
            {
                return;
            }

            DBComponent dbComponent;
            try
            {
                dbComponent = dbManagerComponent.GetZoneDB(root.Fiber.Zone);
            }
            catch (Exception e)
            {
                Log.Warning(
                    $"ServiceDiscoveryAgent read master from mongo failed scene: {rootName} error: {e.Message}");
                return;
            }
            ServiceDiscoveryMaster masterRecord;
            try
            {
                masterRecord = await dbComponent.Query<ServiceDiscoveryMaster>(
                    ServiceDiscoveryPersistenceConst.MasterRecordId, ServiceDiscoveryPersistenceConst.MasterCollection);
            }
            catch (Exception e)
            {
                Log.Warning(
                    $"ServiceDiscoveryAgent read master from mongo failed scene: {rootName} error: {e.Message}");
                return;
            }

            using (masterRecord)
            {
                self = selfRef;
                if (self == null || masterRecord == null || masterRecord.ActorId == default)
                {
                    return;
                }

                long now = self.GetSingleton<TimeInfo>().ServerNow();
                if (masterRecord.LeaseExpireTime <= now)
                {
                    return;
                }

                if (masterRecord.Epoch > 0 && self.CurrentMasterEpoch > 0 && masterRecord.Epoch < self.CurrentMasterEpoch)
                {
                    return;
                }

                if (masterRecord.Epoch > self.CurrentMasterEpoch)
                {
                    self.CurrentMasterEpoch = masterRecord.Epoch;
                }

                self.SwitchToEndpoint(masterRecord.ActorId, "master-record");
            }
        }

        /// <summary>
        /// 切换主节点目标 ActorId，清除已注册状态并移除旧主心跳组件。
        /// </summary>
        private static void SwitchToEndpoint(this ServiceDiscoveryAgent self, ActorId targetActorId, string reason)
        {
            if (self.ServiceDiscoveryActorId == targetActorId)
            {
                return;
            }

            ActorId oldActorId = self.ServiceDiscoveryActorId;
            self.ServiceDiscoveryActorId = targetActorId;
            self.ClearStatus(ServiceDiscoveryAgentStatus.AgentRegistered);
            self.RemoveMasterHeartbeatComponent();
            Log.Info(
                $"ServiceDiscoveryAgent switch endpoint scene: {self.Root().Name} reason: {reason} from: {oldActorId} to: {targetActorId}");
        }

        /// <summary>
        /// 标记在当前主节点端点上需要重新进行全量注册（例如增量同步遇到非主备切换错误时）。
        /// </summary>
        private static void MarkNeedFullRegisterOnCurrentEndpoint(this ServiceDiscoveryAgent self, string reason)
        {
            if (self == null)
            {
                return;
            }

            ActorId currentActorId = self.ServiceDiscoveryActorId;
            self.ClearStatus(ServiceDiscoveryAgentStatus.AgentRegistered);
            self.RemoveMasterHeartbeatComponent();
            self.TriggerBackgroundRegister();
            Log.Info(
                $"ServiceDiscoveryAgent mark full register required scene: {self.Root().Name} reason: {reason} current: {currentActorId}");
        }

        /// <summary>
        /// 使当前缓存的主节点 ActorId 失效，清除已注册状态并触发后台重新解析 Master 与全量注册。
        /// </summary>
        public static void InvalidateEndpointAndTriggerBackgroundRegister(this ServiceDiscoveryAgent self, string reason)
        {
            if (self == null)
            {
                return;
            }

            ActorId oldActorId = self.ServiceDiscoveryActorId;
            self.ServiceDiscoveryActorId = default;
            self.ClearStatus(ServiceDiscoveryAgentStatus.AgentRegistered);
            self.RemoveMasterHeartbeatComponent();
            self.TriggerBackgroundRegister();
            Log.Info(
                $"ServiceDiscoveryAgent invalidate endpoint scene: {self.Root().Name} reason: {reason} old: {oldActorId}");
        }

        /// <summary>
        /// 带主备故障转移重试（Failover）地向主服务发现节点转发请求。
        /// </summary>
        public static async ETTask<T> ForwardToDiscoveryByFailover<T>(this ServiceDiscoveryAgent self, IRequest request,
            string requestName) where T : class, IResponse
        {
            EntityRef<ServiceDiscoveryAgent> selfRef = self;
            await self.EnsureReadyForRequestAsync(requestName);
            self = selfRef;
            if (self == null)
            {
                throw new Exception("service discovery agent disposed");
            }

            return await self.ForwardToDiscoveryByFailoverCore<T>(request, requestName);
        }

        /// <summary>
        /// 向主服务发现节点发送 RPC 请求的核心循环（最多尝试 4 次，遇到主节点不可用时自动触发重新解析）。
        /// </summary>
        private static async ETTask<T> ForwardToDiscoveryByFailoverCore<T>(this ServiceDiscoveryAgent self, IRequest request,
            string requestName) where T : class, IResponse
        {
            EntityRef<ServiceDiscoveryAgent> selfRef = self;
            const int maxAttemptCount = 4;
            for (int attempt = 0; attempt < maxAttemptCount; ++attempt)
            {
                self = selfRef;
                if (self == null)
                {
                    throw new Exception("service discovery agent disposed");
                }

                if (self.ServiceDiscoveryActorId == default)
                {
                    await self.EnsureReadyForRequestAsync(requestName);
                    self = selfRef;
                    if (self == null)
                    {
                        throw new Exception("service discovery agent disposed");
                    }

                    if (!self.IsReady() || self.ServiceDiscoveryActorId == default)
                    {
                        throw new Exception(
                            $"service discovery endpoint unavailable after resolve, request: {requestName}, scene: {self.Root().Name}");
                    }

                    continue;
                }

                ActorId targetActorId = self.ServiceDiscoveryActorId;
                try
                {
                    IResponse rawResponse = await self.MessageSender.Call(targetActorId, request);
                    self = selfRef;
                    if (self == null)
                    {
                        throw new Exception("service discovery agent disposed");
                    }

                    if (rawResponse is not T response)
                    {
                        (rawResponse as MessageObject)?.Dispose();
                        throw new Exception(
                            $"service discovery response type mismatch, request: {requestName}, actual: {rawResponse?.GetType().Name}");
                    }

                    if (response.Error != ErrorCode.ERR_Success)
                    {
                        int error = response.Error;
                        string message = response.Message;
                        if (response is MessageObject messageObject)
                        {
                            messageObject.Dispose();
                        }
                        throw new RpcException(error,
                            $"service discovery response error, request: {requestName}, error: {error}, message: {message}");
                    }

                    return response;
                }
                catch (Exception e)
                {
                    self = selfRef;
                    if (self == null)
                    {
                        throw;
                    }

                    bool shouldResolveMaster = self.ShouldResolveMasterAfterFailure(e);
                    if (shouldResolveMaster)
                    {
                        self.InvalidateEndpointAndTriggerBackgroundRegister("call-failure");
                    }
                    else
                    {
                        Log.Warning(
                            $"ServiceDiscoveryAgent call failed without endpoint fallback scene: {self.Root().Name} request: {requestName} target: {targetActorId} error: {e.Message}");
                        throw;
                    }
                }

                int delay = GetMasterResolveRetryDelay(attempt);
                await self.Root().TimerComponent.WaitAsync(delay);
            }

            throw new Exception($"service discovery request retry exhausted, request: {requestName}");
        }

        /// <summary>
        /// 判断异常是否属于需要重新解析主节点端点的故障转移错误。
        /// </summary>
        private static bool ShouldResolveMasterAfterFailure(this ServiceDiscoveryAgent self, Exception e)
        {

            if (e is RpcException rpcException)
            {
                return ServiceDiscoveryErrorHelper.ShouldTriggerFailover(rpcException.Error);
            }

            return true;
        }

        /// <summary>
        /// 根据重试次数计算主节点解析的指数退避等待毫秒数。
        /// </summary>
        private static int GetMasterResolveRetryDelay(int retry)
        {
            if (retry <= 0)
            {
                return MasterResolveRetryBaseInterval;
            }

            long delay = (long)MasterResolveRetryBaseInterval << retry;
            if (delay >= MasterResolveRetryMaxInterval)
            {
                return MasterResolveRetryMaxInterval;
            }

            return (int)delay;
        }

        /// <summary>
        /// 向主节点发送 <see cref="ServiceAgentRegisterRequest"/> 全量注册本进程服务，并用主节点返回的全网服务快照重建本地缓存及回放差异给本地 Proxy 订阅者。
        /// </summary>
        private static async ETTask<bool> TryRegisterAgentToDiscoveryAsync(this ServiceDiscoveryAgent self,
            Dictionary<string, int> versionSnapshot)
        {
            if (self == null)
            {
                return false;
            }

            if (self.ServiceDiscoveryActorId == default)
            {
                return false;
            }

            EntityRef<ServiceDiscoveryAgent> selfRef = self;
            ActorId targetActorId = self.ServiceDiscoveryActorId;
            ServiceAgentRegisterRequest request = ServiceAgentRegisterRequest.Create();
            request.AgentActorId = self.Root().GetActorId();
            Address ownedAddress = request.AgentActorId.Address;
            if (!ServiceDiscoveryHelper.TryValidateRequiredActorId(request.AgentActorId, nameof(ServiceAgentRegisterRequest),
                    nameof(request.AgentActorId), out string errorMessage))
            {
                throw new ArgumentException(errorMessage);
            }

            self.AppendOwnedLocalServices(request);

            try
            {
                TimerComponent timer = self.Root().TimerComponent;
                ETTask<IResponse> registerTask = self.MessageSender.Call(targetActorId, request);
                IResponse rawResponse = await registerTask;
                self = selfRef;
                if (self == null)
                {
                    (rawResponse as MessageObject)?.Dispose();
                    return false;
                }

                if (rawResponse is not ServiceAgentRegisterResponse response)
                {
                    (rawResponse as MessageObject)?.Dispose();
                    Log.Warning(
                        $"ServiceDiscoveryAgent register to master response mismatch scene: {self.Root().Name} target: {targetActorId} actual: {rawResponse?.GetType().Name}");
                    self.InvalidateEndpointAndTriggerBackgroundRegister("register-response-mismatch");
                    return false;
                }

                if (response.Error != ErrorCode.ERR_Success)
                {
                    Log.Warning(
                        $"ServiceDiscoveryAgent register to master failed scene: {self.Root().Name} target: {targetActorId} error: {response.Error} message: {response.Message}");
                    bool shouldTriggerFailover = ServiceDiscoveryErrorHelper.ShouldTriggerFailover(response.Error);
                    response.Dispose();
                    if (shouldTriggerFailover)
                    {
                        self.InvalidateEndpointAndTriggerBackgroundRegister("register-failed");
                    }

                    return false;
                }

                Dictionary<string, (ActorId ActorId, StringKV Metadata)> oldServices = self.SnapshotLocalServices();
                Dictionary<string, (ActorId ActorId, StringKV Metadata)> newServices = new();
                foreach (ServiceInfoProto serviceInfoProto in response.Services)
                {
                    if (serviceInfoProto == null || string.IsNullOrEmpty(serviceInfoProto.SceneName) ||
                        serviceInfoProto.ActorId == default)
                    {
                        continue;
                    }

                    newServices[serviceInfoProto.SceneName] =
                        (serviceInfoProto.ActorId, ServiceDiscoveryHelper.CloneMetadata(serviceInfoProto.Metadata));
                }

                self.MergeCurrentOwnedLocalServicesIntoSnapshot(ownedAddress, newServices);
                self.ClearLocalServices();
                foreach ((string sceneName, (ActorId actorId, StringKV metadata)) in newServices)
                {
                    self.AddOrUpdateLocalService(sceneName, actorId, metadata);
                }

                self.ReplayResyncDiffToProxySubscribers(oldServices, newServices);
                response.Dispose();
                self.SetStatus(ServiceDiscoveryAgentStatus.AgentRegistered);
                self.MarkPublishedScenesSynced(versionSnapshot);
                self.EnsureMasterHeartbeatComponent();
                return true;
            }
            catch (Exception e)
            {
                self = selfRef;
                if (self != null)
                {
                    Log.Warning(
                        $"ServiceDiscoveryAgent register to master deferred scene: {self.Root().Name} target: {targetActorId} error: {e.Message}");
                    self.InvalidateEndpointAndTriggerBackgroundRegister("register-exception");
                }

                return false;
            }
            finally
            {
                request.Dispose();
            }
        }

        /// <summary>
        /// 尝试将当前所有脏场景（DirtyPublishedScenes）的增量变更逐个同步到主节点。
        /// </summary>
        private static async ETTask<bool> TryFlushDirtyScenesOnceAsync(this ServiceDiscoveryAgent self)
        {
            if (self == null || !self.IsReady())
            {
                return true;
            }

            EntityRef<ServiceDiscoveryAgent> selfRef = self;
            List<(string SceneName, int Version, bool Exists, ActorId ActorId, StringKV Metadata)> snapshots =
                self.CaptureDirtySceneSnapshots();
            if (snapshots.Count == 0)
            {
                return true;
            }

            foreach ((string sceneName, int version, bool exists, ActorId actorId, StringKV metadata) in snapshots)
            {
                self = selfRef;
                if (self == null || !self.IsReady())
                {
                    return true;
                }

                bool synced = exists
                    ? await self.TrySyncRegisterSceneAsync(sceneName, version, actorId, metadata)
                    : await self.TrySyncUnregisterSceneAsync(sceneName, version);
                self = selfRef;
                if (self == null)
                {
                    return true;
                }

                if (!synced)
                {
                    return true;
                }
            }

            self = selfRef;
            return self == null || !self.HasDirtyPublishedScenes();
        }

        /// <summary>
        /// 尝试向主节点增量同步单个本地场景的服务注册。
        /// </summary>
        private static async ETTask<bool> TrySyncRegisterSceneAsync(this ServiceDiscoveryAgent self, string sceneName, int version,
            ActorId actorId, StringKV metadata)
        {
            if (self == null || string.IsNullOrEmpty(sceneName) || actorId == default || version <= 0)
            {
                return true;
            }

            if (self.ServiceDiscoveryActorId == default)
            {
                self.MarkNeedFullRegisterOnCurrentEndpoint($"incremental-register-endpoint-missing:{sceneName}");
                return false;
            }

            EntityRef<ServiceDiscoveryAgent> selfRef = self;
            ActorId targetActorId = self.ServiceDiscoveryActorId;
            ServiceRegisterRequest request = ServiceRegisterRequest.Create();
            request.SceneName = sceneName;
            request.ActorId = actorId;
            if (metadata != null)
            {
                foreach ((string key, string value) in metadata)
                {
                    request.Metadata[key] = value;
                }
            }

            try
            {
                TimerComponent timer = self.Root().TimerComponent;
                ETTask<IResponse> registerTask = self.MessageSender.Call(targetActorId, request);
                IResponse rawResponse = await registerTask;
                self = selfRef;
                if (self == null)
                {
                    (rawResponse as MessageObject)?.Dispose();
                    return false;
                }

                if (rawResponse is not ServiceRegisterResponse response)
                {
                    (rawResponse as MessageObject)?.Dispose();
                    throw new Exception(
                        $"service discovery incremental register response mismatch scene: {sceneName}, actual: {rawResponse?.GetType().Name}");
                }

                if (response.Error != ErrorCode.ERR_Success)
                {
                    int error = response.Error;
                    string message = response.Message;
                    response.Dispose();
                    throw new RpcException(error,
                        $"service discovery incremental register failed scene: {sceneName}, error: {error}, message: {message}");
                }

                response.Dispose();
                self.MarkPublishedSceneSynced(sceneName, version);
                return true;
            }
            catch (Exception e)
            {
                self = selfRef;
                if (self != null)
                {
                    Log.Warning(
                        $"ServiceDiscoveryAgent incremental register failed agent: {self.Root().Name} scene: {sceneName} target: {targetActorId} error: {e.Message}");
                    self.HandleIncrementalSyncFailure(e, $"incremental-register:{sceneName}");
                }

                return false;
            }
            finally
            {
                request.Dispose();
            }
        }

        /// <summary>
        /// 尝试向主节点增量同步单个本地场景的服务注销。
        /// </summary>
        private static async ETTask<bool> TrySyncUnregisterSceneAsync(this ServiceDiscoveryAgent self, string sceneName, int version)
        {
            if (self == null || string.IsNullOrEmpty(sceneName) || version <= 0)
            {
                return true;
            }

            if (self.ServiceDiscoveryActorId == default)
            {
                self.MarkNeedFullRegisterOnCurrentEndpoint($"incremental-unregister-endpoint-missing:{sceneName}");
                return false;
            }

            EntityRef<ServiceDiscoveryAgent> selfRef = self;
            ActorId targetActorId = self.ServiceDiscoveryActorId;
            ServiceUnregisterRequest request = ServiceUnregisterRequest.Create();
            request.SceneName = sceneName;

            try
            {
                TimerComponent timer = self.Root().TimerComponent;
                ETTask<IResponse> unregisterTask = self.MessageSender.Call(targetActorId, request);
                IResponse rawResponse = await unregisterTask;
                self = selfRef;
                if (self == null)
                {
                    (rawResponse as MessageObject)?.Dispose();
                    return false;
                }

                if (rawResponse is not ServiceUnregisterResponse response)
                {
                    (rawResponse as MessageObject)?.Dispose();
                    throw new Exception(
                        $"service discovery incremental unregister response mismatch scene: {sceneName}, actual: {rawResponse?.GetType().Name}");
                }

                if (response.Error != ErrorCode.ERR_Success)
                {
                    int error = response.Error;
                    string message = response.Message;
                    response.Dispose();
                    throw new RpcException(error,
                        $"service discovery incremental unregister failed scene: {sceneName}, error: {error}, message: {message}");
                }

                response.Dispose();
                self.MarkPublishedSceneSynced(sceneName, version);
                return true;
            }
            catch (Exception e)
            {
                self = selfRef;
                if (self != null)
                {
                    Log.Warning(
                        $"ServiceDiscoveryAgent incremental unregister failed agent: {self.Root().Name} scene: {sceneName} target: {targetActorId} error: {e.Message}");
                    self.HandleIncrementalSyncFailure(e, $"incremental-unregister:{sceneName}");
                }

                return false;
            }
            finally
            {
                request.Dispose();
            }
        }

        /// <summary>
        /// 处理增量同步异常：若属于主节点切换错误则使端点失效并重新选主注册，否则在当前端点重新发起全量注册。
        /// </summary>
        private static void HandleIncrementalSyncFailure(this ServiceDiscoveryAgent self, Exception e, string reason)
        {
            if (self == null)
            {
                return;
            }

            bool shouldResolveMaster = self.ShouldResolveMasterAfterFailure(e);
            if (shouldResolveMaster)
            {
                self.InvalidateEndpointAndTriggerBackgroundRegister(reason);
                return;
            }

            self.MarkNeedFullRegisterOnCurrentEndpoint(reason);
        }

        /// <summary>
        /// 新增或更新 Agent 本地的服务缓存及二级索引（通过 Proto 对象）。
        /// </summary>
        private static void AddOrUpdateLocalService(this ServiceDiscoveryAgent self, ServiceInfoProto serviceInfoProto)
        {
            if (serviceInfoProto == null || string.IsNullOrEmpty(serviceInfoProto.SceneName))
            {
                return;
            }

            self.AddOrUpdateLocalService(serviceInfoProto.SceneName, serviceInfoProto.ActorId, serviceInfoProto.Metadata);
        }

        /// <summary>
        /// 新增或更新 Agent 本地的服务缓存实体（<see cref="ServiceInfo"/>）并维护二级索引。
        /// </summary>
        private static void AddOrUpdateLocalService(this ServiceDiscoveryAgent self, string sceneName, ActorId actorId, StringKV metadata)
        {
            if (string.IsNullOrEmpty(sceneName) || actorId == default)
            {
                return;
            }

            if (self.HasSameLocalService(sceneName, actorId, metadata))
            {
                return;
            }

            self.RemoveLocalService(sceneName);

            ServiceInfo serviceInfo = self.AddChild<ServiceInfo, string, ActorId>(sceneName, actorId);
            serviceInfo.Metadata = ServiceDiscoveryHelper.CloneMetadata(metadata);
            self.SceneNameServices[serviceInfo.SceneName] = serviceInfo;
            ServiceDiscoveryHelper.AddToIndexes(self.ServicesIndexs, self.Indexs, serviceInfo.SceneName, serviceInfo.Metadata);
        }

        /// <summary>
        /// 当同进程业务场景注册服务时，更新本进程发布表与本地缓存，并立即通知同进程内匹配的订阅者。
        /// </summary>
        public static void UpsertLocalServiceAfterRegister(this ServiceDiscoveryAgent self, string sceneName, ActorId actorId,
            StringKV metadata)
        {
            self.UpsertOwnedLocalService(sceneName, actorId, metadata);
            bool changed = !self.HasSameLocalService(sceneName, actorId, metadata);
            if (!changed)
            {
                return;
            }

            self.AddOrUpdateLocalService(sceneName, actorId, metadata);
            using ServiceInfoProto serviceInfoProto = ServiceDiscoveryHelper.CreateServiceInfoProto(sceneName, actorId, metadata);
            self.NotifyProxySubscribers(1, serviceInfoProto);
        }

        /// <summary>
        /// 当同进程业务场景注销服务时，通知同进程订阅者下线，并从本进程发布表和本地缓存中移除。
        /// </summary>
        public static void RemoveLocalServiceAfterUnregister(this ServiceDiscoveryAgent self, string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                return;
            }

            if (self.TryCreateLocalServiceSnapshot(sceneName, out ServiceInfoProto localServiceSnapshot))
            {
                using (localServiceSnapshot)
                {
                    self.NotifyProxySubscribers(2, localServiceSnapshot);
                }
            }

            self.RemoveOwnedLocalService(sceneName);
            self.RemoveLocalService(sceneName);
        }

        /// <summary>
        /// 从 Agent 本地服务缓存与索引中移除指定名称的服务。
        /// </summary>
        private static void RemoveLocalService(this ServiceDiscoveryAgent self, string sceneName)
        {
            ServiceDiscoveryHelper.RemoveLocalServiceCache(self.SceneNameServices, self.ServicesIndexs, self.Indexs, sceneName);
        }

        /// <summary>
        /// 清空 Agent 本地的全部服务缓存与索引。
        /// </summary>
        private static void ClearLocalServices(this ServiceDiscoveryAgent self)
        {
            using ListComponent<string> sceneNames = ListComponent<string>.Create();
            foreach ((string sceneName, EntityRef<ServiceInfo> _) in self.SceneNameServices)
            {
                sceneNames.Add(sceneName);
            }

            foreach (string sceneName in sceneNames)
            {
                self.RemoveLocalService(sceneName);
            }
        }

        /// <summary>
        /// 对当前本地缓存的所有服务制作一份快照（用于在全量重同步后对比差异）。
        /// </summary>
        private static Dictionary<string, (ActorId ActorId, StringKV Metadata)> SnapshotLocalServices(this ServiceDiscoveryAgent self)
        {
            Dictionary<string, (ActorId ActorId, StringKV Metadata)> snapshot = new();
            foreach ((string sceneName, EntityRef<ServiceInfo> serviceRef) in self.SceneNameServices)
            {
                ServiceInfo serviceInfo = serviceRef;
                if (serviceInfo == null || string.IsNullOrEmpty(sceneName) || serviceInfo.ActorId == default)
                {
                    continue;
                }

                snapshot[sceneName] = (serviceInfo.ActorId, ServiceDiscoveryHelper.CloneMetadata(serviceInfo.Metadata));
            }

            return snapshot;
        }

        /// <summary>
        /// 将本进程权威的已发布服务列表（<see cref="ServiceDiscoveryAgent.LocalPublishedServices"/>）合并覆盖到主节点返回的快照中，防止全量注册期间发生的本地增删被旧快照冲掉。
        /// </summary>
        private static void MergeCurrentOwnedLocalServicesIntoSnapshot(this ServiceDiscoveryAgent self, Address ownedAddress,
            Dictionary<string, (ActorId ActorId, StringKV Metadata)> services)
        {
            if (self == null || services == null || ownedAddress == default)
            {
                return;
            }

            using ListComponent<string> staleSceneNames = ListComponent<string>.Create();
            foreach ((string sceneName, (ActorId actorId, StringKV _)) in services)
            {
                if (string.IsNullOrEmpty(sceneName) || actorId == default || actorId.Address != ownedAddress)
                {
                    continue;
                }

                if (!self.LocalPublishedServices.TryGetValue(sceneName,
                        out (ActorId ActorId, StringKV Metadata) localService) ||
                    localService.ActorId == default ||
                    localService.ActorId.Address != ownedAddress)
                {
                    staleSceneNames.Add(sceneName);
                }
            }

            foreach (string staleSceneName in staleSceneNames)
            {
                services.Remove(staleSceneName);
            }

            foreach ((string sceneName, (ActorId actorId, StringKV metadata)) in self.LocalPublishedServices)
            {
                if (string.IsNullOrEmpty(sceneName) || actorId == default || actorId.Address != ownedAddress)
                {
                    continue;
                }

                services[sceneName] = (actorId, ServiceDiscoveryHelper.CloneMetadata(metadata));
            }
        }

        /// <summary>
        /// 捕获当前本进程已发布服务的版本号快照。
        /// </summary>
        private static Dictionary<string, int> CapturePublishedSceneVersions(this ServiceDiscoveryAgent self)
        {
            Dictionary<string, int> snapshot = new();
            if (self == null)
            {
                return snapshot;
            }

            foreach ((string sceneName, int version) in self.PublishedSceneVersions)
            {
                if (string.IsNullOrEmpty(sceneName) || version <= 0)
                {
                    continue;
                }

                snapshot[sceneName] = version;
            }

            return snapshot;
        }

        /// <summary>
        /// 捕获所有待增量同步的脏场景快照列表。
        /// </summary>
        private static List<(string SceneName, int Version, bool Exists, ActorId ActorId, StringKV Metadata)>
            CaptureDirtySceneSnapshots(this ServiceDiscoveryAgent self)
        {
            List<(string SceneName, int Version, bool Exists, ActorId ActorId, StringKV Metadata)> snapshots = new();
            if (self == null)
            {
                return snapshots;
            }

            foreach (string sceneName in self.DirtyPublishedScenes)
            {
                if (string.IsNullOrEmpty(sceneName))
                {
                    continue;
                }

                if (!self.PublishedSceneVersions.TryGetValue(sceneName, out int version) || version <= 0)
                {
                    continue;
                }

                if (!self.LocalPublishedServices.TryGetValue(sceneName, out (ActorId ActorId, StringKV Metadata) localService)
                    || localService.ActorId == default)
                {
                    snapshots.Add((sceneName, version, false, default, null));
                    continue;
                }

                snapshots.Add((sceneName, version, true, localService.ActorId,
                    ServiceDiscoveryHelper.CloneMetadata(localService.Metadata)));
            }

            return snapshots;
        }

        /// <summary>
        /// 判断本地缓存中是否已存在完全一致（ActorId 与 Metadata 均相同）的同名服务。
        /// </summary>
        private static bool HasSameLocalService(this ServiceDiscoveryAgent self, string sceneName, ActorId actorId, StringKV metadata)
        {
            if (self == null || string.IsNullOrEmpty(sceneName) || actorId == default)
            {
                return false;
            }

            if (!self.SceneNameServices.TryGetValue(sceneName, out EntityRef<ServiceInfo> serviceRef))
            {
                return false;
            }

            ServiceInfo serviceInfo = serviceRef;
            if (serviceInfo == null)
            {
                return false;
            }

            return ServiceDiscoveryHelper.IsSameService(serviceInfo.ActorId, serviceInfo.Metadata, actorId, metadata);
        }

        /// <summary>
        /// 尝试为指定本地缓存的服务创建一份 <see cref="ServiceInfoProto"/> 快照。
        /// </summary>
        private static bool TryCreateLocalServiceSnapshot(this ServiceDiscoveryAgent self, string sceneName,
            out ServiceInfoProto serviceInfoProto)
        {
            serviceInfoProto = null;
            if (self == null || string.IsNullOrEmpty(sceneName))
            {
                return false;
            }

            if (!self.SceneNameServices.TryGetValue(sceneName, out EntityRef<ServiceInfo> serviceRef))
            {
                return false;
            }

            ServiceInfo serviceInfo = serviceRef;
            if (serviceInfo == null || serviceInfo.ActorId == default)
            {
                return false;
            }

            serviceInfoProto = ServiceDiscoveryHelper.CreateServiceInfoProto(sceneName, serviceInfo.ActorId, serviceInfo.Metadata);
            return true;
        }

        /// <summary>
        /// 将本进程所有已发布的本地服务填充到 <see cref="ServiceAgentRegisterRequest.LocalServices"/> 中。
        /// </summary>
        private static void AppendOwnedLocalServices(this ServiceDiscoveryAgent self, ServiceAgentRegisterRequest request)
        {
            if (self == null || request == null)
            {
                return;
            }

            Address ownedAddress = request.AgentActorId.Address;
            foreach ((string sceneName, (ActorId actorId, StringKV metadata)) in self.LocalPublishedServices)
            {
                if (string.IsNullOrEmpty(sceneName) || actorId == default || actorId.Address != ownedAddress)
                {
                    continue;
                }

                request.LocalServices.Add(ServiceDiscoveryHelper.CreateServiceInfoProto(sceneName, actorId, metadata));
            }
        }

        /// <summary>
        /// 递增指定本地发布场景的版本号并将其加入待同步脏集合。
        /// </summary>
        private static void MarkLocalPublishedSceneDirty(this ServiceDiscoveryAgent self, string sceneName)
        {
            if (self == null || string.IsNullOrEmpty(sceneName))
            {
                return;
            }

            if (!self.PublishedSceneVersions.TryGetValue(sceneName, out int version))
            {
                version = 0;
            }

            unchecked
            {
                ++version;
            }

            if (version <= 0)
            {
                version = 1;
            }

            self.PublishedSceneVersions[sceneName] = version;
            self.DirtyPublishedScenes.Add(sceneName);
        }

        /// <summary>
        /// 若指定场景的当前版本号与已同步版本号一致，则清除其 Dirty 标记。
        /// </summary>
        private static void MarkPublishedSceneSynced(this ServiceDiscoveryAgent self, string sceneName, int version)
        {
            if (self == null || string.IsNullOrEmpty(sceneName) || version <= 0)
            {
                return;
            }

            if (!self.PublishedSceneVersions.TryGetValue(sceneName, out int currentVersion) || currentVersion != version)
            {
                return;
            }

            self.DirtyPublishedScenes.Remove(sceneName);
            if (!self.LocalPublishedServices.ContainsKey(sceneName))
            {
                self.PublishedSceneVersions.Remove(sceneName);
            }
        }

        /// <summary>
        /// 批量标记快照中的场景版本已同步完成。
        /// </summary>
        private static void MarkPublishedScenesSynced(this ServiceDiscoveryAgent self, Dictionary<string, int> versionSnapshot)
        {
            if (self == null || versionSnapshot == null)
            {
                return;
            }

            foreach ((string sceneName, int version) in versionSnapshot)
            {
                self.MarkPublishedSceneSynced(sceneName, version);
            }
        }

        /// <summary>
        /// 标记指定本地场景为 Dirty，并根据 Agent 是否就绪触发增量同步或全量注册。
        /// </summary>
        private static void ScheduleLocalPublishedSceneSync(this ServiceDiscoveryAgent self, string sceneName)
        {
            if (self == null || string.IsNullOrEmpty(sceneName))
            {
                return;
            }

            self.MarkLocalPublishedSceneDirty(sceneName);
            if (self.IsReady())
            {
                self.TriggerBackgroundMutationSync();
                return;
            }

            self.TriggerBackgroundRegister();
        }

        /// <summary>
        /// 更新本进程权威发布表中的服务信息并调度后台同步。
        /// </summary>
        private static void UpsertOwnedLocalService(this ServiceDiscoveryAgent self, string sceneName, ActorId actorId,
            StringKV metadata)
        {
            if (string.IsNullOrEmpty(sceneName) || actorId == default)
            {
                return;
            }

            StringKV copiedMetadata = ServiceDiscoveryHelper.CloneMetadata(metadata);
            if (self.LocalPublishedServices.TryGetValue(sceneName, out (ActorId ActorId, StringKV Metadata) current) &&
                ServiceDiscoveryHelper.IsSameService(current.ActorId, current.Metadata, actorId, copiedMetadata))
            {
                return;
            }

            self.LocalPublishedServices[sceneName] = (actorId, copiedMetadata);
            self.ScheduleLocalPublishedSceneSync(sceneName);
        }

        /// <summary>
        /// 从本进程权威发布表中移除指定服务并调度后台同步。
        /// </summary>
        private static void RemoveOwnedLocalService(this ServiceDiscoveryAgent self, string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                return;
            }

            if (!self.LocalPublishedServices.Remove(sceneName))
            {
                return;
            }

            self.ScheduleLocalPublishedSceneSync(sceneName);
        }

        /// <summary>
        /// 当 Agent 与主节点重新全量同步后，对比新旧服务列表差异，向同进程内的 Proxy 订阅者补发增删通知。
        /// </summary>
        private static void ReplayResyncDiffToProxySubscribers(this ServiceDiscoveryAgent self,
            Dictionary<string, (ActorId ActorId, StringKV Metadata)> oldServices,
            Dictionary<string, (ActorId ActorId, StringKV Metadata)> newServices)
        {
            if (oldServices == null || newServices == null || self.ProxySubscribers.Count == 0)
            {
                return;
            }

            foreach ((string sceneName, (ActorId oldActorId, StringKV oldMetadata)) in oldServices)
            {
                if (string.IsNullOrEmpty(sceneName) || oldActorId == default || newServices.ContainsKey(sceneName))
                {
                    continue;
                }

                using ServiceInfoProto removeInfo = ServiceDiscoveryHelper.CreateServiceInfoProto(sceneName, oldActorId, oldMetadata);
                self.NotifyProxySubscribers(2, removeInfo);
            }

            foreach ((string sceneName, (ActorId newActorId, StringKV newMetadata)) in newServices)
            {
                if (string.IsNullOrEmpty(sceneName) || newActorId == default)
                {
                    continue;
                }

                if (oldServices.TryGetValue(sceneName, out (ActorId ActorId, StringKV Metadata) oldService) &&
                    ServiceDiscoveryHelper.IsSameService(oldService.ActorId, oldService.Metadata, newActorId, newMetadata))
                {
                    continue;
                }

                // 同名服务发生变化时，先用旧元数据回放remove，再用新元数据回放add，
                // 避免订阅过滤条件变化导致proxy残留旧缓存。
                if (oldServices.TryGetValue(sceneName, out oldService))
                {
                    using ServiceInfoProto oldServiceInfo =
                        ServiceDiscoveryHelper.CreateServiceInfoProto(sceneName, oldService.ActorId, oldService.Metadata);
                    self.NotifyProxySubscribers(2, oldServiceInfo);
                }

                using ServiceInfoProto newServiceInfo =
                    ServiceDiscoveryHelper.CreateServiceInfoProto(sceneName, newActorId, newMetadata);
                self.NotifyProxySubscribers(1, newServiceInfo);
            }
        }

        /// <summary>
        /// 在 Agent 侧登记同进程 Proxy 的服务变更订阅过滤条件，并返回当前匹配的服务快照。
        /// </summary>
        public static int SubscribeProxyServiceChange(this ServiceDiscoveryAgent self, string sceneName, ActorId subscriberActorId,
            string filterName, StringKV filterMetadata, List<ServiceInfoProto> snapshot)
        {
            if (subscriberActorId == default)
            {
                return ErrorCode.ERR_SubscriberActorIdRequired;
            }

            filterMetadata ??= new StringKV();

            self.ProxySubscribers[sceneName] = subscriberActorId;
            if (self.ProxySubscriberFilters.ContainSubKey(sceneName, filterName))
            {
                self.ProxySubscriberFilters.Remove(sceneName, filterName);
            }

            self.ProxySubscriberFilters.Add(sceneName, filterName, ServiceDiscoveryHelper.CloneMetadata(filterMetadata));

            List<ServiceInfo> matchedServices = self.GetServiceInfoByFilter(filterMetadata);
            if (snapshot != null)
            {
                foreach (ServiceInfo serviceInfo in matchedServices)
                {
                    snapshot.Add(serviceInfo.ToProto());
                }
            }

            return ErrorCode.ERR_Success;
        }

        /// <summary>
        /// 取消同进程指定 Proxy 场景的所有服务变更订阅。
        /// </summary>
        public static void UnsubscribeProxyServiceChange(this ServiceDiscoveryAgent self, string sceneName)
        {
            self.ProxySubscriberFilters.Remove(sceneName);
            self.ProxySubscribers.Remove(sceneName);
        }

        /// <summary>
        /// 遍历本进程内所有 Proxy 订阅者，若变更的服务元数据匹配其任一过滤条件，则向该 Proxy 发送 <see cref="ServiceChangeNotification"/>。
        /// </summary>
        private static void NotifyProxySubscribers(this ServiceDiscoveryAgent self, int changeType, ServiceInfoProto serviceInfoProto)
        {
            if (serviceInfoProto == null || string.IsNullOrEmpty(serviceInfoProto.SceneName) || serviceInfoProto.ActorId == default)
            {
                return;
            }

            MessageSender sender = self.Root().GetComponent<MessageSender>();
            if (sender == null)
            {
                return;
            }

            foreach ((string sceneName, var filters) in self.ProxySubscriberFilters)
            {
                if (!self.ProxySubscribers.TryGetValue(sceneName, out ActorId subscriberActorId) || subscriberActorId == default)
                {
                    continue;
                }

                bool matched = false;
                foreach ((string _, StringKV filter) in filters)
                {
                    if (!ServiceDiscoveryHelper.MatchesMetadataFilter(serviceInfoProto.Metadata, filter))
                    {
                        continue;
                    }

                    matched = true;
                    break;
                }

                if (!matched)
                {
                    continue;
                }

                ServiceChangeNotification notification = ServiceChangeNotification.Create();
                notification.ChangeType = changeType;
                notification.Epoch = self.CurrentMasterEpoch;
                notification.MasterActorId = self.ServiceDiscoveryActorId;

                notification.ServiceInfo.Add(ServiceDiscoveryHelper.CreateServiceInfoProto(serviceInfoProto.SceneName,
                    serviceInfoProto.ActorId, serviceInfoProto.Metadata));
                sender.Send(subscriberActorId, notification);
            }
        }

        /// <summary>
        /// 处理来自主节点的 <see cref="ServiceChangeNotification"/> 通知：先校验 Fencing Epoch，通过后再更新本地缓存并扇出给本地 Proxy。
        /// </summary>
        public static void OnServiceChangeNotification(this ServiceDiscoveryAgent self, ServiceChangeNotification message)
        {
            if (self == null || message == null || !self.IsReady())
            {
                return;
            }

            if (!self.ShouldAcceptServiceChangeNotification(message))
            {
                return;
            }

            self.OnServiceChangeNotification(message.ChangeType, message.ServiceInfo);
        }

        /// <summary>
        /// 校验服务变更通知的 Fencing Epoch 与 MasterActorId：
        /// 拒绝旧世代（Epoch 较小）的脑裂脏消息；若收到更高 Epoch 的通知则立即使当前端点失效并触发重连新主。
        /// </summary>
        private static bool ShouldAcceptServiceChangeNotification(this ServiceDiscoveryAgent self, ServiceChangeNotification message)
        {
            if (message == null)
            {
                return false;
            }

            if (message.Epoch <= 0 || message.MasterActorId == default)
            {
                Log.Warning(
                    $"ServiceDiscoveryAgent ignore notification with invalid fencing scene: {self.Root().Name} epoch: {message.Epoch} masterActorId: {message.MasterActorId}");
                return false;
            }

            long currentEpoch = self.CurrentMasterEpoch;
            ActorId currentMasterActorId = self.ServiceDiscoveryActorId;
            if (currentEpoch <= 0 || currentMasterActorId == default)
            {
                Log.Warning(
                    $"ServiceDiscoveryAgent ignore notification because local master view is incomplete scene: {self.Root().Name} currentEpoch: {currentEpoch} currentMaster: {currentMasterActorId}");
                return false;
            }

            if (message.Epoch < currentEpoch)
            {
                Log.Warning(
                    $"ServiceDiscoveryAgent ignore stale notification scene: {self.Root().Name} messageEpoch: {message.Epoch} currentEpoch: {currentEpoch} messageMaster: {message.MasterActorId} currentMaster: {currentMasterActorId}");
                return false;
            }

            if (message.Epoch > currentEpoch)
            {
                Log.Warning(
                    $"ServiceDiscoveryAgent receive newer epoch notification scene: {self.Root().Name} messageEpoch: {message.Epoch} currentEpoch: {currentEpoch} messageMaster: {message.MasterActorId} currentMaster: {currentMasterActorId}");
                self.InvalidateEndpointAndTriggerBackgroundRegister("notification-newer-epoch");
                return false;
            }

            if (message.MasterActorId != currentMasterActorId)
            {
                Log.Warning(
                    $"ServiceDiscoveryAgent ignore notification from unexpected master scene: {self.Root().Name} epoch: {message.Epoch} messageMaster: {message.MasterActorId} currentMaster: {currentMasterActorId}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 应用单条服务变更（1=新增/更新，2=移除）到 Agent 本地缓存，并扇出给本地 Proxy 订阅者。
        /// </summary>
        private static void OnServiceChangeNotification(this ServiceDiscoveryAgent self, int changeType, ServiceInfoProto serviceInfoProto)
        {
            if (!self.IsReady())
            {
                return;
            }

            switch (changeType)
            {
                case 1:
                    if (self.HasSameLocalService(serviceInfoProto.SceneName, serviceInfoProto.ActorId, serviceInfoProto.Metadata))
                    {
                        return;
                    }

                    self.AddOrUpdateLocalService(serviceInfoProto);
                    self.NotifyProxySubscribers(1, serviceInfoProto);
                    break;
                case 2:
                    if (serviceInfoProto != null)
                    {
                        if (!self.SceneNameServices.ContainsKey(serviceInfoProto.SceneName))
                        {
                            return;
                        }

                        self.NotifyProxySubscribers(2, serviceInfoProto);
                        self.RemoveLocalService(serviceInfoProto.SceneName);
                    }

                    break;
            }
        }

        /// <summary>
        /// 批量应用服务变更列表。
        /// </summary>
        public static void OnServiceChangeNotification(this ServiceDiscoveryAgent self, int changeType,
            List<ServiceInfoProto> serviceInfos)
        {
            if (serviceInfos == null)
            {
                return;
            }

            foreach (ServiceInfoProto serviceInfo in serviceInfos)
            {
                self.OnServiceChangeNotification(changeType, serviceInfo);
            }
        }

        /// <summary>
        /// 根据元数据过滤条件查询 Agent 本地缓存的服务列表（支持二级索引加速）。
        /// </summary>
        public static List<ServiceInfo> GetServiceInfoByFilter(this ServiceDiscoveryAgent self, StringKV filterMetadata)
        {
            return ServiceDiscoveryHelper.GetServiceInfoByFilter(self.SceneNameServices, self.ServicesIndexs, filterMetadata);
        }

        /// <summary>
        /// 确保当前 Agent 已完成向主节点注册并处于就绪状态。
        /// </summary>
        public static async ETTask EnsureReadyAsync(this ServiceDiscoveryAgent self, string requestName)
        {
            await self.EnsureReadyForRequestAsync(requestName);
        }

        /// <summary>
        /// 异步等待 Agent 就绪后，按元数据过滤条件查询本地服务缓存。
        /// </summary>
        public static async ETTask<List<ServiceInfo>> QueryLocalByFilterAsync(this ServiceDiscoveryAgent self,
            StringKV filterMetadata)
        {
            EntityRef<ServiceDiscoveryAgent> selfRef = self;
            await self.EnsureReadyForRequestAsync(nameof(ServiceQueryRequest));
            self = selfRef;
            if (self == null)
            {
                return new List<ServiceInfo>();
            }

            return self.GetServiceInfoByFilter(filterMetadata);
        }
    }
}

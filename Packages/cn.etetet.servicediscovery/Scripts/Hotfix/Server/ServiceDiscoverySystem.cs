using System;
using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 服务发现中心核心组件系统（<see cref="ServiceDiscovery"/>）。
    /// 负责：
    /// 1. 主备高可用选举与租约续期（基于 MongoDB `FindOneAndReplace` 原子 CAS 与单调递增 Fencing Epoch）；
    /// 2. 接收各进程 <see cref="ServiceDiscoveryAgent"/> 的全量注册、增量服务增删与聚合心跳；
    /// 3. 将全网服务变更通过去重缓冲队列（<see cref="ServiceDiscoveryNotificationBufferComponent"/>）按批次推送给所有在线 Agent。
    /// </summary>
    [EntitySystemOf(typeof(ServiceDiscovery))]
    public static partial class ServiceDiscoverySystem
    {
        /// <summary>主节点租约抢占结果：失败</summary>
        private const int MasterLeaseAcquireFailed = 0;
        /// <summary>主节点租约抢占结果：当前主节点成功续租</summary>
        private const int MasterLeaseAcquireRenewed = 1;
        /// <summary>主节点租约抢占结果：新节点成功接管（Takeover，Epoch + 1）</summary>
        private const int MasterLeaseAcquireTakeover = 2;

        [EntitySystem]
        private static void Awake(this ServiceDiscovery self)
        {
            long now = self.GetMonotonicServerNow();
            self.GetOrAddLease().LastMasterLeaseCheckTime = now;
            self.GetOrAddNotificationBuffer().LastNotificationFlushTime = now;
            self.SyncHeartbeatCheckerComponent();
        }

        [EntitySystem]
        private static void Destroy(this ServiceDiscovery self)
        {
            Scene root = self.Root();
            if (root == null)
            {
                return;
            }
            self.FlushPendingServiceChangeNotifications(true);
            self.ClearPendingServiceChangeBuffer();
            self.RemoveComponent<ServiceDiscoveryHeartbeatChecker>();
        }

        /// <summary>
        /// 每帧驱动：同步心跳检查器挂载状态、刷出到期的服务变更通知批次，并在非单机模式下定期执行 MongoDB 主节点租约续期/接管检查。
        /// </summary>
        [EntitySystem]
        private static void Update(this ServiceDiscovery self)
        {
            self.SyncHeartbeatCheckerComponent();
            long now = self.GetMonotonicServerNow();
            self.FlushPendingServiceChangeNotifications(false);

            if (self.IsMongoDisabled())
            {
                return;
            }

            if (self.IsLeaseCircuitOpen(now))
            {
                return;
            }

            if (now < self.GetOrAddLease().NextLeaseRetryTime)
            {
                return;
            }

            if (now - self.GetOrAddLease().LastMasterLeaseCheckTime >= self.GetOrAddLease().MasterLeaseRenewInterval)
            {
                self.GetOrAddLease().LastMasterLeaseCheckTime = now;
                self.TickMasterLeaseAsync().Coroutine();
            }
        }

        /// <summary>
        /// 初始化主备角色信息，并从Mongo加载状态
        /// </summary>
        public static async ETTask InitializeAsync(this ServiceDiscovery self)
        {
            EntityRef<ServiceDiscovery> selfRef = self;
            long now = self.GetMonotonicServerNow();
            self.GetOrAddLease().LastMasterLeaseCheckTime = now;
            self.GetOrAddNotificationBuffer().LastNotificationFlushTime = now;
            self.SyncHeartbeatCheckerComponent();

            ServiceDiscoveryBootstrapSingleton bootstrap = self.GetServiceDiscoveryBootstrap();
            if (bootstrap != null)
            {
                ServiceDiscoveryLeaseComponent lease = self.GetOrAddLease();
                Scene root = self.Root();

                lease.CurrentMasterSceneName = root.Name;
                lease.CurrentMasterActorId = root.GetActorId();
                lease.CurrentMasterEpoch = 1;
                lease.CurrentMasterLeaseExpireTime = long.MaxValue;
                lease.IsActiveMaster = true;
                lease.LastMasterLeaseCheckTime = now;
                lease.NextLeaseRetryTime = 0;
                lease.LeaseTickRunning = false;
                lease.LeaseFailureCount = 0;
                lease.LeaseCircuitOpenUntil = 0;
                bootstrap.SetMasterActorId(lease.CurrentMasterActorId);

                self.SyncHeartbeatCheckerComponent();
                Log.Info(
                    $"ServiceDiscovery init activeRole: LocalSingleMaster scene: {root.Name} currentMaster: {lease.CurrentMasterSceneName}");
                await ETTask.CompletedTask;
                return;
            }

            await self.EnsureActiveMasterAsync();
            self = selfRef;
            if (self == null)
            {
                return;
            }

            string activeRole = self.GetOrAddLease().IsActiveMaster ? "ActiveMaster" : "Follower";
            Log.Info(
                $"ServiceDiscovery init activeRole: {activeRole} scene: {self.Root().Name} currentMaster: {self.GetOrAddLease().CurrentMasterSceneName}");
        }

        /// <summary>
        /// 确保当前节点是可写主节点。
        /// </summary>
        public static async ETTask<bool> EnsureActiveMasterAsync(this ServiceDiscovery self)
        {
            return await self.EnsureActiveMasterCoreAsync(false);
        }

        /// <summary>
        /// 确保当前节点是可读写主节点（带fencing强校验）。
        /// 每次请求都会尝试通过CAS续租，避免旧主在抢主后误接受请求。
        /// </summary>
        public static async ETTask<bool> EnsureActiveMasterWithFenceAsync(this ServiceDiscovery self)
        {
            return await self.EnsureActiveMasterCoreAsync(true);
        }

        /// <summary>
        /// 在协程锁保护下校验或抢占主节点租约的核心实现。
        /// </summary>
        /// <param name="self">当前服务发现组件</param>
        /// <param name="enforceFenceRenew">是否强制执行一次 CAS 续租以提供 Fencing 强隔离保证</param>
        private static async ETTask<bool> EnsureActiveMasterCoreAsync(this ServiceDiscovery self, bool enforceFenceRenew)
        {
            if (self == null)
            {
                return false;
            }

            EntityRef<ServiceDiscovery> selfRef = self;
            using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.ServiceDiscoveryMasterLease, self.Id))
            {
                self = selfRef;
                if (self == null)
                {
                    return false;
                }

                if (self.IsMongoDisabled())
                {
                    return self.GetOrAddLease().IsActiveMaster;
                }

                long now = self.GetMonotonicServerNow();
                if (self.IsLeaseCircuitOpen(now) || now < self.GetOrAddLease().NextLeaseRetryTime)
                {
                    return false;
                }

                bool hasMaster = await self.RefreshMasterFromDbAsync();

                self = selfRef;
                if (self == null)
                {
                    return false;
                }

                if (self.GetOrAddLease().IsActiveMaster)
                {
                    now = self.GetMonotonicServerNow();
                    bool shouldRenew = enforceFenceRenew || now + self.GetOrAddLease().MasterLeaseRenewInterval >= self.GetOrAddLease().CurrentMasterLeaseExpireTime;
                    if (!shouldRenew)
                    {
                        return true;
                    }

                    bool renewed = await self.TryRenewMasterLeaseAsync();
                    self = selfRef;
                    if (self == null)
                    {
                        return false;
                    }

                    if (renewed)
                    {
                        return true;
                    }

                    await self.RefreshMasterFromDbAsync();
                    self = selfRef;
                    return self != null && self.GetOrAddLease().IsActiveMaster;
                }

                if (hasMaster)
                {
                    return false;
                }

                bool promoted = await self.PromoteToMasterAsync();
                self = selfRef;
                if (self == null)
                {
                    return false;
                }

                return promoted && self.GetOrAddLease().IsActiveMaster;
            }
        }

        /// <summary>
        /// 晋升为活跃主节点：通过Mongo原子CAS抢占主租约，成功后清空易失状态并等待Agent重放。
        /// </summary>
        public static async ETTask<bool> PromoteToMasterAsync(this ServiceDiscovery self)
        {
            EntityRef<ServiceDiscovery> selfRef = self;
            int acquireState = await self.TryAcquireMasterLeaseAsync();

            self = selfRef;
            if (self == null || acquireState == MasterLeaseAcquireFailed)
            {
                return false;
            }

            if (!self.GetOrAddLease().IsActiveMaster)
            {
                return false;
            }

            self.ClearEphemeralState();

            if (acquireState == MasterLeaseAcquireTakeover)
            {
                Log.Debug(
                    $"ServiceDiscovery promote to master scene: {self.GetOrAddLease().CurrentMasterSceneName} actorId: {self.GetOrAddLease().CurrentMasterActorId} epoch: {self.GetOrAddLease().CurrentMasterEpoch}");
            }

            return true;
        }

        /// <summary>
        /// 注册服务
        /// </summary>
        public static async ETTask RegisterServiceAsync(this ServiceDiscovery self, string sceneName, ActorId actorId, StringKV metadata)
        {
            ServiceInfo serviceInfo = await self.UpsertServiceAsync(sceneName, actorId, metadata, true);
            if (serviceInfo != null)
            {
                Log.Debug($"Service registered: {sceneName} actorId: {actorId}");
            }
        }

        /// <summary>
        /// 注销服务
        /// </summary>
        public static async ETTask UnregisterServiceAsync(this ServiceDiscovery self, string sceneName,
            bool requireActiveMaster = false)
        {
            if (requireActiveMaster)
            {
                EntityRef<ServiceDiscovery> checkRef = self;
                bool isMaster = await self.EnsureActiveMasterWithFenceAsync();
                self = checkRef;
                if (self == null || !isMaster)
                {
                    return;
                }
            }

            bool removed = await self.RemoveServiceAsync(sceneName, true);
            if (removed)
            {
                Log.Debug($"Service unregistered: {sceneName}");
            }
        }

        /// <summary>
        /// 注册 Agent（按进程地址聚合）
        /// </summary>
        public static async ETTask<List<ServiceInfoProto>> RegisterAgentAsync(this ServiceDiscovery self, ActorId agentActorId,
            List<ServiceInfoProto> localServices)
        {
            List<ServiceInfoProto> snapshot = new();
            if (agentActorId == default)
            {
                return snapshot;
            }

            EntityRef<ServiceDiscovery> selfRef = self;
            long now = self.GetMonotonicServerNow();
            self.AgentActorIds.Remove(agentActorId.Address);
            await self.ApplyAgentLocalServicesAsync(agentActorId, localServices, now);
            self = selfRef;
            if (self == null)
            {
                return snapshot;
            }

            self.TouchAgentHeartbeat(agentActorId.Address, now);
            self.AgentActorIds[agentActorId.Address] = agentActorId;

            foreach ((string sceneName, EntityRef<ServiceInfo> serviceRef) in self.Services)
            {
                ServiceInfo serviceInfo = serviceRef;
                if (serviceInfo == null || string.IsNullOrEmpty(sceneName) || serviceInfo.ActorId == default)
                {
                    continue;
                }

                snapshot.Add(serviceInfo.ToProto());
            }

            await ETTask.CompletedTask;
            return snapshot;
        }

        /// <summary>
        /// 更新 Agent 心跳（按进程地址聚合）
        /// </summary>
        public static async ETTask UpdateAgentHeartbeatAsync(this ServiceDiscovery self, ActorId agentActorId)
        {
            if (agentActorId == default)
            {
                return;
            }

            self.TouchAgentHeartbeat(agentActorId.Address, self.GetMonotonicServerNow());
            self.AgentActorIds[agentActorId.Address] = agentActorId;
            await ETTask.CompletedTask;
        }

        /// <summary>
        /// 刷新当前主节点记录
        /// </summary>
        public static async ETTask<bool> RefreshMasterFromDbAsync(this ServiceDiscovery self)
        {
            EntityRef<ServiceDiscovery> selfRef = self;
            DBComponent dbComponent = self.GetPersistenceDB();
            EntityRef<DBComponent> dbComponentRef = dbComponent;

            self = selfRef;
            dbComponent = dbComponentRef;
            if (self == null || dbComponent == null)
            {
                return false;
            }

            using ServiceDiscoveryMaster masterRecord = await dbComponent.Query<ServiceDiscoveryMaster>(
                ServiceDiscoveryPersistenceConst.MasterRecordId, ServiceDiscoveryPersistenceConst.MasterCollection);

            self = selfRef;
            if (self == null)
            {
                return false;
            }

            if (masterRecord == null || string.IsNullOrEmpty(masterRecord.SceneName) || masterRecord.ActorId == default)
            {
                self.GetOrAddLease().CurrentMasterSceneName = null;
                self.GetOrAddLease().CurrentMasterActorId = default;
                self.GetOrAddLease().CurrentMasterEpoch = 0;
                self.GetOrAddLease().CurrentMasterLeaseExpireTime = 0;
                self.GetOrAddLease().IsActiveMaster = false;
                self.SyncHeartbeatCheckerComponent();
                return false;
            }

            long now = self.GetMonotonicServerNow();
            bool leaseValid = masterRecord.LeaseExpireTime > now;
            self.ApplyMasterRecordToLocal(masterRecord);
            return leaseValid;
        }

        /// <summary>
        /// 通知服务变更
        /// </summary>
        private static void NotifyServiceChange(this ServiceDiscovery self, int changeType, ServiceInfo serviceInfo)
        {
            if (serviceInfo == null)
            {
                return;
            }

            self.NotifyServiceChange(changeType, serviceInfo.SceneName, serviceInfo.ActorId, serviceInfo.Metadata);
        }

        /// <summary>
        /// 将指定服务的变更事件放入所有已注册进程 Agent 的待发送通知队列。
        /// </summary>
        private static void NotifyServiceChange(this ServiceDiscovery self, int changeType, string sceneName, ActorId actorId,
            StringKV metadata)
        {
            if (string.IsNullOrEmpty(sceneName) || actorId == default)
            {
                return;
            }

            foreach ((Address address, ActorId agentActorId) in self.AgentActorIds)
            {
                if (address == default || agentActorId == default)
                {
                    continue;
                }

                self.EnqueuePendingServiceChange(agentActorId, changeType, sceneName, actorId, metadata);
            }
        }

        /// <summary>
        /// 将服务变更加入指定订阅者的待发送缓冲字典（按 `changeType:sceneName` 去重合并）。
        /// </summary>
        private static void EnqueuePendingServiceChange(this ServiceDiscovery self, ActorId subscriberActorId, int changeType,
            ServiceInfo serviceInfo)
        {
            if (serviceInfo == null)
            {
                return;
            }

            self.EnqueuePendingServiceChange(subscriberActorId, changeType, serviceInfo.SceneName, serviceInfo.ActorId,
                serviceInfo.Metadata);
        }

        private static void EnqueuePendingServiceChange(this ServiceDiscovery self, ActorId subscriberActorId, int changeType,
            string sceneName, ActorId actorId, StringKV metadata)
        {
            if (subscriberActorId == default || string.IsNullOrEmpty(sceneName) || actorId == default)
            {
                return;
            }

            if (!self.GetOrAddNotificationBuffer().PendingNotifications.TryGetValue(subscriberActorId,
                    out Dictionary<string, (int ChangeType, string SceneName, ActorId ActorId, StringKV Metadata)> pending))
            {
                pending = new Dictionary<string, (int ChangeType, string SceneName, ActorId ActorId, StringKV Metadata)>();
                self.GetOrAddNotificationBuffer().PendingNotifications.Add(subscriberActorId, pending);
            }

            string dedupeKey = $"{changeType}:{sceneName}";
            pending[dedupeKey] = (changeType, sceneName, actorId, ServiceDiscoveryHelper.CloneMetadata(metadata));
        }

        /// <summary>
        /// 达到防抖间隔（或强制刷出）时，将缓冲队列中的服务变更按 <see cref="ServiceDiscoveryNotificationBufferComponent.NotificationBatchMaxItems"/> 拆批发送给各进程 Agent。
        /// </summary>
        private static void FlushPendingServiceChangeNotifications(this ServiceDiscovery self, bool force)
        {
            if (self.GetOrAddNotificationBuffer().PendingNotifications.Count == 0)
            {
                return;
            }

            long now = self.GetMonotonicServerNow();
            if (!force && now - self.GetOrAddNotificationBuffer().LastNotificationFlushTime < self.GetOrAddNotificationBuffer().NotificationDebounceInterval)
            {
                return;
            }

            MessageSender sender = self.Root().GetComponent<MessageSender>();
            int batchMaxItems = self.GetOrAddNotificationBuffer().NotificationBatchMaxItems > 0 ? self.GetOrAddNotificationBuffer().NotificationBatchMaxItems : 128;
            using ListComponent<ActorId> invalidTargets = ListComponent<ActorId>.Create();
            foreach ((ActorId subscriberActorId,
                     Dictionary<string, (int ChangeType, string SceneName, ActorId ActorId, StringKV Metadata)> pending) in
                     self.GetOrAddNotificationBuffer().PendingNotifications)
            {
                if (subscriberActorId == default || pending == null || pending.Count == 0)
                {
                    invalidTargets.Add(subscriberActorId);
                    continue;
                }

                Dictionary<int, List<(string SceneName, ActorId ActorId, StringKV Metadata)>> grouped = new();
                foreach ((string _, (int ChangeType, string SceneName, ActorId ActorId, StringKV Metadata) entry) in pending)
                {
                    if (string.IsNullOrEmpty(entry.SceneName) || entry.ActorId == default)
                    {
                        continue;
                    }

                    if (!grouped.TryGetValue(entry.ChangeType, out List<(string SceneName, ActorId ActorId, StringKV Metadata)> list))
                    {
                        list = new List<(string SceneName, ActorId ActorId, StringKV Metadata)>();
                        grouped.Add(entry.ChangeType, list);
                    }

                    list.Add((entry.SceneName, entry.ActorId, ServiceDiscoveryHelper.CloneMetadata(entry.Metadata)));
                }

                foreach ((int changeType, List<(string SceneName, ActorId ActorId, StringKV Metadata)> serviceInfos) in grouped)
                {
                    int index = 0;
                    while (index < serviceInfos.Count)
                    {
                        ServiceChangeNotification notification = ServiceChangeNotification.Create();
                        notification.ChangeType = changeType;
                        notification.Epoch = self.GetOrAddLease().CurrentMasterEpoch;
                        notification.MasterActorId = self.GetOrAddLease().CurrentMasterActorId;
                        int itemCount = 0;
                        while (index < serviceInfos.Count && itemCount < batchMaxItems)
                        {
                            (string sceneName, ActorId actorId, StringKV metadata) snapshot = serviceInfos[index++];
                            ServiceInfoProto infoProto = ServiceInfoProto.Create();
                            infoProto.SceneName = snapshot.sceneName;
                            infoProto.ActorId = snapshot.actorId;
                            if (snapshot.metadata != null)
                            {
                                foreach ((string key, string value) in snapshot.metadata)
                                {
                                    infoProto.Metadata[key] = value;
                                }
                            }

                            notification.ServiceInfo.Add(infoProto);
                            ++itemCount;
                        }

                        sender.Send(subscriberActorId, notification);
                    }
                }

                pending.Clear();
                invalidTargets.Add(subscriberActorId);
            }

            foreach (ActorId invalidTarget in invalidTargets)
            {
                self.GetOrAddNotificationBuffer().PendingNotifications.Remove(invalidTarget);
            }

            self.GetOrAddNotificationBuffer().LastNotificationFlushTime = now;
        }

        /// <summary>
        /// 清空待发送的服务变更缓冲队列。
        /// </summary>
        private static void ClearPendingServiceChangeBuffer(this ServiceDiscovery self)
        {
            if (self.GetOrAddNotificationBuffer().PendingNotifications.Count == 0)
            {
                return;
            }

            self.GetOrAddNotificationBuffer().PendingNotifications.Clear();
        }

        /// <summary>
        /// 根据当前节点是否为活跃 Master 动态添加或移除 <see cref="ServiceDiscoveryHeartbeatChecker"/> 组件。
        /// </summary>
        private static void SyncHeartbeatCheckerComponent(this ServiceDiscovery self)
        {
            ServiceDiscoveryHeartbeatChecker checker = self.GetComponent<ServiceDiscoveryHeartbeatChecker>();

            if (!self.GetOrAddLease().IsActiveMaster)
            {
                self.ClearPendingServiceChangeBuffer();
                if (checker != null)
                {
                    self.RemoveComponent<ServiceDiscoveryHeartbeatChecker>();
                }

                return;
            }

            if (checker == null)
            {
                self.AddComponent<ServiceDiscoveryHeartbeatChecker>();
            }
        }

        /// <summary>
        /// 周期性执行主节点租约维护：若当前为主节点则尝试续租，若无活跃主节点则尝试晋升接管。
        /// </summary>
        private static async ETTask TickMasterLeaseAsync(this ServiceDiscovery self)
        {
            if (self.GetOrAddLease().LeaseTickRunning)
            {
                return;
            }

            long now = self.GetMonotonicServerNow();
            if (self.IsLeaseCircuitOpen(now) || now < self.GetOrAddLease().NextLeaseRetryTime)
            {
                return;
            }

            self.GetOrAddLease().LeaseTickRunning = true;
            EntityRef<ServiceDiscovery> selfRef = self;
            bool tickSuccess = false;
            bool shouldRecordResult = true;
            try
            {
                using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.ServiceDiscoveryMasterLease, self.Id))
                {
                    self = selfRef;
                    if (self == null)
                    {
                        shouldRecordResult = false;
                        return;
                    }

                    now = self.GetMonotonicServerNow();
                    if (self.IsLeaseCircuitOpen(now) || now < self.GetOrAddLease().NextLeaseRetryTime)
                    {
                        shouldRecordResult = false;
                        return;
                    }

                    bool hasMaster = await self.RefreshMasterFromDbAsync();
                    self = selfRef;
                    if (self == null)
                    {
                        shouldRecordResult = false;
                        return;
                    }

                    if (self.GetOrAddLease().IsActiveMaster)
                    {
                        tickSuccess = await self.TryRenewMasterLeaseAsync();
                        return;
                    }

                    if (!hasMaster)
                    {
                        tickSuccess = await self.PromoteToMasterAsync();
                        return;
                    }

                    tickSuccess = true;
                }
            }
            catch (Exception e)
            {
                self = selfRef;
                if (self != null)
                {
                    self.GetOrAddLease().IsActiveMaster = false;
                    self.SyncHeartbeatCheckerComponent();
                    Log.Warning(
                        $"ServiceDiscovery lease tick failed scene: {self.Root().Name} error: {e.Message}");
                }
            }
            finally
            {
                self = selfRef;
                if (self != null)
                {
                    self.GetOrAddLease().LeaseTickRunning = false;
                    if (shouldRecordResult)
                    {
                        self.OnLeaseTickCompleted(tickSuccess);
                    }
                }
            }
        }

        /// <summary>
        /// 更新主租约巡检结果与熔断退避状态。
        /// </summary>
        private static void OnLeaseTickCompleted(this ServiceDiscovery self, bool success)
        {
            ServiceDiscoveryLeaseComponent lease = self.GetOrAddLease();
            if (success)
            {
                ServiceDiscoveryHelper.ResetRetryState(ref lease.LeaseFailureCount, ref lease.LeaseCircuitOpenUntil,
                    ref lease.NextLeaseRetryTime);
                return;
            }

            long now = self.GetMonotonicServerNow();
            bool circuitOpened = ServiceDiscoveryHelper.RecordRetryFailure(ref lease.LeaseFailureCount,
                ref lease.LeaseCircuitOpenUntil, ref lease.NextLeaseRetryTime, lease.LeaseCircuitThreshold,
                lease.LeaseCircuitOpenDuration, lease.LeaseFailureBackoffBase, lease.LeaseFailureBackoffMax, now);

            if (circuitOpened)
            {
                Log.Warning(
                    $"ServiceDiscovery lease circuit opened scene: {self.Root().Name} failures: {lease.LeaseFailureCount} openUntil: {lease.LeaseCircuitOpenUntil}");
            }
        }

        /// <summary>
        /// 判断主租约访问 MongoDB 的熔断器是否处于打开状态。
        /// </summary>
        private static bool IsLeaseCircuitOpen(this ServiceDiscovery self, long now)
        {
            return self.GetOrAddLease().LeaseCircuitOpenUntil > now;
        }

        /// <summary>
        /// 尝试通过 MongoDB `FindOneAndReplace` 原子 CAS 续期当前主节点租约。
        /// </summary>
        private static async ETTask<bool> TryRenewMasterLeaseAsync(this ServiceDiscovery self)
        {
            EntityRef<ServiceDiscovery> selfRef = self;
            DBComponent dbComponent = self.GetPersistenceDB();
            EntityRef<DBComponent> dbComponentRef = dbComponent;

            self = selfRef;
            dbComponent = dbComponentRef;
            if (self == null || dbComponent == null)
            {
                return false;
            }

            long now = self.GetMonotonicServerNow();
            string sceneName = self.Root().Name;
            ActorId actorId = self.Root().GetActorId();
            long masterId = ServiceDiscoveryPersistenceConst.MasterRecordId;
            long leaseTimeout = self.GetOrAddLease().MasterLeaseTimeout;
            long leaseExpireTime = now + leaseTimeout;

            using ServiceDiscoveryMaster currentRecord =
                await dbComponent.Query<ServiceDiscoveryMaster>(masterId, ServiceDiscoveryPersistenceConst.MasterCollection);
            self = selfRef;
            dbComponent = dbComponentRef;
            if (self == null || dbComponent == null)
            {
                return false;
            }

            if (currentRecord == null || currentRecord.SceneName != sceneName || currentRecord.ActorId != actorId ||
                currentRecord.LeaseExpireTime <= now)
            {
                self.GetOrAddLease().IsActiveMaster = false;
                self.SyncHeartbeatCheckerComponent();
                return false;
            }

            long expectedEpoch = currentRecord.Epoch;
            long expectedLeaseExpireTime = currentRecord.LeaseExpireTime;
            ServiceDiscoveryMaster replacement =
                self.CreateMasterRecordForPersistence(masterId, sceneName, actorId, expectedEpoch, leaseExpireTime,
                    leaseTimeout, now);

            try
            {
                using ServiceDiscoveryMaster updated = await dbComponent.FindOneAndReplace<ServiceDiscoveryMaster>(
                    masterId,
                    d => d.Id == masterId
                        && d.Epoch == expectedEpoch
                        && d.SceneName == sceneName
                        && d.ActorId == actorId
                        && d.LeaseExpireTime == expectedLeaseExpireTime,
                    replacement,
                    false,
                    ServiceDiscoveryPersistenceConst.MasterCollection);

                self = selfRef;
                if (self == null || updated == null)
                {
                    if (self != null)
                    {
                        self.GetOrAddLease().IsActiveMaster = false;
                        self.SyncHeartbeatCheckerComponent();
                    }

                    return false;
                }

                self.ApplyMasterRecordToLocal(updated);
                return self.GetOrAddLease().IsActiveMaster;
            }
            catch (Exception e)
            {
                self = selfRef;
                if (self != null)
                {
                    self.GetOrAddLease().IsActiveMaster = false;
                    self.SyncHeartbeatCheckerComponent();
                    Log.Warning(
                        $"ServiceDiscovery renew master lease failed scene: {self.Root().Name} error: {e.Message}");
                }
                return false;
            }
        }

        /// <summary>
        /// 尝试通过 MongoDB `FindOneAndReplace` 原子 CAS 抢占或接管已过期的主节点租约（接管时将 Epoch + 1）。
        /// </summary>
        private static async ETTask<int> TryAcquireMasterLeaseAsync(this ServiceDiscovery self)
        {
            EntityRef<ServiceDiscovery> selfRef = self;
            DBComponent dbComponent = self.GetPersistenceDB();
            EntityRef<DBComponent> dbComponentRef = dbComponent;

            self = selfRef;
            dbComponent = dbComponentRef;
            if (self == null || dbComponent == null)
            {
                return MasterLeaseAcquireFailed;
            }

            long now = self.GetMonotonicServerNow();
            long masterId = ServiceDiscoveryPersistenceConst.MasterRecordId;
            string sceneName = self.Root().Name;
            ActorId actorId = self.Root().GetActorId();

            using ServiceDiscoveryMaster currentRecord =
                await dbComponent.Query<ServiceDiscoveryMaster>(masterId, ServiceDiscoveryPersistenceConst.MasterCollection);
            self = selfRef;
            dbComponent = dbComponentRef;
            if (self == null || dbComponent == null)
            {
                return MasterLeaseAcquireFailed;
            }

            bool hasCurrent = currentRecord != null && !string.IsNullOrEmpty(currentRecord.SceneName) && currentRecord.ActorId != default;
            bool ownerSelf = hasCurrent && currentRecord.SceneName == sceneName && currentRecord.ActorId == actorId;
            bool leaseAlive = hasCurrent && currentRecord.LeaseExpireTime > now;
            if (hasCurrent && leaseAlive && !ownerSelf)
            {
                self.GetOrAddLease().IsActiveMaster = false;
                self.SyncHeartbeatCheckerComponent();
                return MasterLeaseAcquireFailed;
            }

            bool takeover = !ownerSelf || !leaseAlive;
            long currentEpoch = hasCurrent ? currentRecord.Epoch : 0;
            long expectedLeaseExpireTime = hasCurrent ? currentRecord.LeaseExpireTime : 0;
            long newEpoch = takeover ? currentEpoch + 1 : currentEpoch;
            if (newEpoch <= 0)
            {
                newEpoch = 1;
            }

            long leaseTimeout = self.GetOrAddLease().MasterLeaseTimeout;
            long leaseExpireTime = now + leaseTimeout;
            bool isUpsert = !hasCurrent;

            ServiceDiscoveryMaster replacement =
                self.CreateMasterRecordForPersistence(masterId, sceneName, actorId, newEpoch, leaseExpireTime,
                    leaseTimeout, now);

            try
            {
                ServiceDiscoveryMaster updated;
                if (isUpsert)
                {
                    updated = await dbComponent.FindOneAndReplace<ServiceDiscoveryMaster>(
                        masterId,
                        d => d.Id == masterId && d.Epoch == 0,
                        replacement,
                        true,
                        ServiceDiscoveryPersistenceConst.MasterCollection);
                }
                else if (ownerSelf && leaseAlive)
                {
                    updated = await dbComponent.FindOneAndReplace<ServiceDiscoveryMaster>(
                        masterId,
                        d => d.Id == masterId
                            && d.Epoch == currentEpoch
                            && d.SceneName == sceneName
                            && d.ActorId == actorId
                            && d.LeaseExpireTime == expectedLeaseExpireTime,
                        replacement,
                        false,
                        ServiceDiscoveryPersistenceConst.MasterCollection);
                }
                else
                {
                    updated = await dbComponent.FindOneAndReplace<ServiceDiscoveryMaster>(
                        masterId,
                        d => d.Id == masterId
                            && d.Epoch == currentEpoch
                            && d.LeaseExpireTime == expectedLeaseExpireTime
                            && d.LeaseExpireTime <= now,
                        replacement,
                        false,
                        ServiceDiscoveryPersistenceConst.MasterCollection);
                }

                self = selfRef;
                dbComponent = dbComponentRef;
                using (updated)
                {
                    if (self == null || updated == null)
                    {
                        if (self != null)
                        {
                            self.GetOrAddLease().IsActiveMaster = false;
                            self.SyncHeartbeatCheckerComponent();
                        }

                        return MasterLeaseAcquireFailed;
                    }

                    self.ApplyMasterRecordToLocal(updated);
                    return takeover ? MasterLeaseAcquireTakeover : MasterLeaseAcquireRenewed;
                }
            }
            catch (Exception e)
            {
                self = selfRef;
                if (self != null)
                {
                    self.GetOrAddLease().IsActiveMaster = false;
                    self.SyncHeartbeatCheckerComponent();
                    if (!e.Message.Contains("E11000"))
                    {
                        Log.Warning(
                            $"ServiceDiscovery acquire master lease failed scene: {self.Root().Name} error: {e.Message}");
                    }
                }

                return MasterLeaseAcquireFailed;
            }
        }

        /// <summary>
        /// 将 MongoDB 中的主节点租约记录应用到本地 <see cref="ServiceDiscoveryLeaseComponent"/> 状态，并同步心跳检查器。
        /// </summary>
        private static void ApplyMasterRecordToLocal(this ServiceDiscovery self, ServiceDiscoveryMaster masterRecord)
        {
            self.GetOrAddLease().CurrentMasterSceneName = masterRecord.SceneName;
            self.GetOrAddLease().CurrentMasterActorId = masterRecord.ActorId;
            self.GetOrAddLease().CurrentMasterEpoch = masterRecord.Epoch;
            self.GetOrAddLease().CurrentMasterLeaseExpireTime = masterRecord.LeaseExpireTime;

            long now = self.GetMonotonicServerNow();
            Scene root = self.Root();
            self.GetOrAddLease().IsActiveMaster = masterRecord.LeaseExpireTime > now
                                  && root.Name == masterRecord.SceneName
                                  && root.GetActorId() == masterRecord.ActorId;
            self.SyncHeartbeatCheckerComponent();
        }

        /// <summary>
        /// 在内存中新增或更新 <see cref="ServiceInfo"/> 实体，并在发生变更时按需入队通知。
        /// </summary>
        private static ServiceInfo UpsertServiceInMemory(this ServiceDiscovery self, string sceneName, ActorId actorId, StringKV metadata,
            bool notify, long registerTime, long lastHeartbeatTime, out bool changed)
        {
            changed = false;
            if (metadata == null)
            {
                metadata = new StringKV();
            }

            long now = self.GetMonotonicServerNow();
            if (self.Services.TryGetValue(sceneName, out EntityRef<ServiceInfo> serviceRef))
            {
                ServiceInfo existing = serviceRef;
                if (existing != null && ServiceDiscoveryHelper.IsSameService(existing, actorId, metadata))
                {
                    existing.LastHeartbeatTime = lastHeartbeatTime > 0 ? lastHeartbeatTime : now;
                    if (registerTime > 0)
                    {
                        existing.RegisterTime = registerTime;
                    }

                    return existing;
                }

                self.RemoveServiceInMemory(sceneName, false);
            }

            changed = true;

            ServiceInfo serviceInfo = self.AddChild<ServiceInfo, string, ActorId>(sceneName, actorId);
            ServiceDiscoveryHelper.CopyMetadata(metadata, serviceInfo.Metadata);

            serviceInfo.RegisterTime = registerTime > 0 ? registerTime : now;
            serviceInfo.LastHeartbeatTime = lastHeartbeatTime > 0 ? lastHeartbeatTime : now;
            self.Services[sceneName] = serviceInfo;

            if (notify)
            {
                self.NotifyServiceChange(1, serviceInfo);
            }

            return serviceInfo;
        }

        /// <summary>
        /// 清空内存中的所有 <see cref="ServiceInfo"/> 实体。
        /// </summary>
        private static void ClearServicesInMemory(this ServiceDiscovery self)
        {
            using ListComponent<string> sceneNames = ListComponent<string>.Create();
            foreach ((string sceneName, EntityRef<ServiceInfo> _) in self.Services)
            {
                sceneNames.Add(sceneName);
            }

            foreach (string sceneName in sceneNames)
            {
                self.RemoveServiceInMemory(sceneName, false);
            }
        }

        /// <summary>
        /// 清空主节点的所有内存易失态（在刚晋升为新 Master 时调用，等待各进程 Agent 重新全量注册上报）。
        /// </summary>
        private static void ClearEphemeralState(this ServiceDiscovery self)
        {
            self.ClearServicesInMemory();
            self.GetOrAddAgentHeartbeat().AgentHeartbeatTimes.Clear();
            self.AgentActorIds.Clear();
            self.AgentOwnedSceneNames.Clear();
            self.ClearPendingServiceChangeBuffer();
        }

        /// <summary>
        /// 应用指定进程 Agent 全量注册上报的本进程服务列表：先清理该进程之前登记但本次已不存在的陈旧服务，再逐一 Upsert 本次上报的服务。
        /// </summary>
        private static async ETTask ApplyAgentLocalServicesAsync(this ServiceDiscovery self, ActorId agentActorId,
            List<ServiceInfoProto> localServices, long now)
        {
            Dictionary<string, (ActorId ActorId, StringKV Metadata)> incomingServices = new();
            if (localServices != null)
            {
                foreach (ServiceInfoProto serviceInfoProto in localServices)
                {
                    if (serviceInfoProto == null || string.IsNullOrEmpty(serviceInfoProto.SceneName) ||
                        serviceInfoProto.ActorId == default || serviceInfoProto.ActorId.Address != agentActorId.Address)
                    {
                        continue;
                    }

                    incomingServices[serviceInfoProto.SceneName] =
                        (serviceInfoProto.ActorId, ServiceDiscoveryHelper.CloneMetadata(serviceInfoProto.Metadata));
                }
            }

            using ListComponent<string> staleScenes = ListComponent<string>.Create();
            if (self.AgentOwnedSceneNames.TryGetValue(agentActorId.Address, out HashSet<string> trackedSceneNames))
            {
                foreach (string sceneName in trackedSceneNames)
                {
                    if (!incomingServices.ContainsKey(sceneName))
                    {
                        staleScenes.Add(sceneName);
                    }
                }
            }

            EntityRef<ServiceDiscovery> selfRef = self;
            foreach (string sceneName in staleScenes)
            {
                self = selfRef;
                if (self == null)
                {
                    return;
                }
                await self.RemoveServiceAsync(sceneName, true);
            }

            foreach ((string sceneName, (ActorId actorId, StringKV metadata)) in incomingServices)
            {
                self = selfRef;
                if (self == null)
                {
                    return;
                }
                await self.UpsertServiceAsync(sceneName, actorId, metadata, true, now);
            }

            self = selfRef;
            self.SetTrackedOwnedScenes(agentActorId.Address, incomingServices.Keys);
        }

        /// <summary>
        /// 获取当前区服用于主节点租约持久化的 <see cref="DBComponent"/>。
        /// </summary>
        private static DBComponent GetPersistenceDB(this ServiceDiscovery self)
        {
            Scene root = self.Root();
            DBManagerComponent dbManagerComponent = root.GetComponent<DBManagerComponent>();
            if (dbManagerComponent == null)
            {
                throw new Exception($"service discovery db manager not found scene: {root.Name}");
            }

            return dbManagerComponent.GetZoneDB(root.Fiber.Zone);
        }

        /// <summary>
        /// 判断是否处于禁用 MongoDB 的本地单机 Bootstrap 模式。
        /// </summary>
        private static bool IsMongoDisabled(this ServiceDiscovery self)
        {
            return self.GetServiceDiscoveryBootstrap() != null;
        }

        /// <summary>
        /// 获取进程内单机直连引导单例（若未启用则返回 null）。
        /// </summary>
        private static ServiceDiscoveryBootstrapSingleton GetServiceDiscoveryBootstrap(this ServiceDiscovery self)
        {
            return self.Fiber().GetSingleton<ServiceDiscoveryBootstrapSingleton>();
        }

        /// <summary>
        /// 在按场景名哈希分片的协程锁保护下更新或新增服务，并在同名服务元数据变化时先广播旧快照移除通知、再广播新快照新增通知。
        /// </summary>
        private static async ETTask<ServiceInfo> UpsertServiceAsync(this ServiceDiscovery self, string sceneName, ActorId actorId,
            StringKV metadata, bool notify, long now = 0)
        {
            EntityRef<ServiceDiscovery> selfRef = self;
            using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.ServiceDiscoveryServiceMutation,
                       GetServiceMutationLockKey(sceneName)))
            {
                self = selfRef;
                if (self == null)
                {
                    return null;
                }

                bool hasOldSnapshot = false;
                ActorId oldActorId = default;
                StringKV oldMetadata = null;
                if (self.Services.TryGetValue(sceneName, out EntityRef<ServiceInfo> oldServiceRef))
                {
                    ServiceInfo oldService = oldServiceRef;
                    if (oldService != null && !ServiceDiscoveryHelper.IsSameService(oldService, actorId, metadata))
                    {
                        hasOldSnapshot = true;
                        oldActorId = oldService.ActorId;
                        oldMetadata = ServiceDiscoveryHelper.CloneMetadata(oldService.Metadata);
                    }
                }

                if (now <= 0)
                {
                    now = self.GetMonotonicServerNow();
                }

                bool changed;
                ServiceInfo serviceInfo = self.UpsertServiceInMemory(sceneName, actorId, metadata, false, 0, now, out changed);
                self.TouchAgentHeartbeat(actorId.Address, now);
                if (!notify || !changed || serviceInfo == null)
                {
                    return serviceInfo;
                }

                if (hasOldSnapshot)
                {
                    self.NotifyServiceChange(2, sceneName, oldActorId, oldMetadata);
                }

                self.NotifyServiceChange(1, serviceInfo);
                return serviceInfo;
            }
        }

        /// <summary>
        /// 在按场景名哈希分片的协程锁保护下移除指定服务。
        /// </summary>
        private static async ETTask<bool> RemoveServiceAsync(this ServiceDiscovery self, string sceneName, bool notify)
        {
            EntityRef<ServiceDiscovery> selfRef = self;
            using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.ServiceDiscoveryServiceMutation,
                       GetServiceMutationLockKey(sceneName)))
            {
                self = selfRef;
                if (self == null)
                {
                    return false;
                }

                return self.RemoveServiceInMemory(sceneName, notify);
            }
        }

        /// <summary>
        /// 从内存中移除并销毁指定名称的 <see cref="ServiceInfo"/>，并按需入队下线通知。
        /// </summary>
        private static bool RemoveServiceInMemory(this ServiceDiscovery self, string sceneName, bool notify)
        {
            if (!self.Services.TryGetValue(sceneName, out EntityRef<ServiceInfo> serviceRef))
            {
                return false;
            }

            ServiceInfo serviceInfo = serviceRef;
            self.Services.Remove(sceneName);

            if (notify && serviceInfo != null)
            {
                self.NotifyServiceChange(2, serviceInfo);
            }

            if (serviceInfo != null && serviceInfo.ActorId != default)
            {
                self.UntrackOwnedScene(serviceInfo.ActorId.Address, sceneName);
            }

            serviceInfo?.Dispose();
            return serviceInfo != null;
        }

        /// <summary>
        /// 刷新指定进程地址（<see cref="Address"/>）的最后心跳时间戳。
        /// </summary>
        private static void TouchAgentHeartbeat(this ServiceDiscovery self, Address address, long heartbeatTime)
        {
            if (address == default)
            {
                return;
            }

            self.GetOrAddAgentHeartbeat().AgentHeartbeatTimes[address] = heartbeatTime;
        }

        /// <summary>
        /// 获取当前 ServiceDiscovery 节点的单调递增服务端毫秒时间戳。
        /// </summary>
        public static long GetMonotonicServerNow(this ServiceDiscovery self)
        {
            ServiceDiscoveryRuntimeStateComponent runtimeState = self.GetOrAddRuntimeState();
            return ServiceDiscoveryHelper.GetMonotonicNow(self.Root(), "ServiceDiscovery", ref runtimeState.LastRawNow,
                ref runtimeState.MonotonicNow);
        }

        /// <summary>
        /// 计算服务增删协程锁的 Key。
        /// </summary>
        private static long GetServiceMutationLockKey(string sceneName)
        {
            return sceneName.GetLongHashCode();
        }

        /// <summary>
        /// 记录指定进程地址所拥有的全部服务场景名称集合。
        /// </summary>
        private static void SetTrackedOwnedScenes(this ServiceDiscovery self, Address address, IEnumerable<string> sceneNames)
        {
            if (address == default)
            {
                return;
            }

            if (sceneNames == null)
            {
                self.AgentOwnedSceneNames.Remove(address);
                return;
            }

            HashSet<string> trackedSceneNames = new();
            foreach (string sceneName in sceneNames)
            {
                if (!string.IsNullOrEmpty(sceneName))
                {
                    trackedSceneNames.Add(sceneName);
                }
            }

            if (trackedSceneNames.Count == 0)
            {
                self.AgentOwnedSceneNames.Remove(address);
                return;
            }

            self.AgentOwnedSceneNames[address] = trackedSceneNames;
        }

        /// <summary>
        /// 从进程地址拥有的服务集合中移除单个已注销的场景名。
        /// </summary>
        private static void UntrackOwnedScene(this ServiceDiscovery self, Address address, string sceneName)
        {
            if (address == default || string.IsNullOrEmpty(sceneName))
            {
                return;
            }

            if (!self.AgentOwnedSceneNames.TryGetValue(address, out HashSet<string> trackedSceneNames))
            {
                return;
            }

            trackedSceneNames.Remove(sceneName);
            if (trackedSceneNames.Count == 0)
            {
                self.AgentOwnedSceneNames.Remove(address);
            }
        }

        /// <summary>
        /// 创建或复用用于写入 MongoDB 的 <see cref="ServiceDiscoveryMaster"/> 租约实体记录。
        /// </summary>
        private static ServiceDiscoveryMaster CreateMasterRecordForPersistence(this ServiceDiscovery self, long recordId,
            string sceneName, ActorId actorId, long epoch, long leaseExpireTime, long leaseTimeout, long updateTime)
        {
            ServiceDiscoveryMaster record = self.GetComponent<ServiceDiscoveryMaster>() ?? self.AddComponentWithId<ServiceDiscoveryMaster>(recordId);
            record.SceneName = sceneName;
            record.ActorId = actorId;
            record.Epoch = epoch;
            record.LeaseExpireTime = leaseExpireTime;
            record.LeaseTimeout = leaseTimeout;
            record.UpdateTime = updateTime;
            return record;
        }

    }
}

using System;
using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 单个实体 ID（Key）的位置路由记录实体系统（<see cref="LocationInfo"/>）。
    /// 每个 <see cref="LocationInfo"/> 以实体 ID 作为自身 Id，内部按 <c>locationType</c>（如 Unit、Player、GateSession）存储对应的 <see cref="LocationTypeState"/>。
    /// </summary>
    [EntitySystemOf(typeof(LocationInfo))]
    public static partial class LocationInfoSystem
    {
        [EntitySystem]
        private static void Awake(this LocationInfo self)
        {
            self.TypeStates ??= new Dictionary<int, LocationTypeState>();
        }

        [EntitySystem]
        private static void Destroy(this LocationInfo self)
        {
            self.TypeStates?.Clear();
        }
    }

    /// <summary>
    /// 位置服务核心组件系统（<see cref="LocationComponent"/>）。
    /// 负责维护全区可迁移实体（ActorLocation）的实时 <see cref="ActorId"/> 映射、迁移期间的位置锁（LockToken + 超时保护）、MongoDB 持久化回退，以及基于服务发现的主备切换（Primary/Backup）。
    /// </summary>
    [EntitySystemOf(typeof(LocationComponent))]
    public static partial class LocationComponentSystem
    {
        /// <summary>
        /// 监听服务新增事件，重新计算当前 Location 节点是否为主节点（Primary）。
        /// </summary>
        [Event(SceneType.All)]
        public class OnServiceChangeAddService_RefreshLocationPrimary: AEvent<Scene, OnServiceChangeAddService>
        {
            protected override async ETTask Run(Scene scene, OnServiceChangeAddService args)
            {
                LocationComponent locationComponent = scene.GetComponent<LocationComponent>();
                if (locationComponent != null)
                {
                    locationComponent.RefreshPrimaryState();
                }

                await ETTask.CompletedTask;
            }
        }

        /// <summary>
        /// 监听服务移除事件，重新计算当前 Location 节点是否为主节点（Primary）。
        /// </summary>
        [Event(SceneType.All)]
        public class OnServiceChangeRemoveService_RefreshLocationPrimary: AEvent<Scene, OnServiceChangeRemoveService>
        {
            protected override async ETTask Run(Scene scene, OnServiceChangeRemoveService args)
            {
                LocationComponent locationComponent = scene.GetComponent<LocationComponent>();
                if (locationComponent != null)
                {
                    locationComponent.RefreshPrimaryState();
                }

                await ETTask.CompletedTask;
            }
        }

        [EntitySystem]
        private static void Awake(this LocationComponent self)
        {
            self.IsPrimaryLocation = false;
            self.PrimaryLocationSceneName = string.Empty;
        }

        /// <summary>
        /// 获取 Location 路由数据在 MongoDB 中的集合名称。
        /// </summary>
        private static string GetCollectionName(this LocationComponent self)
        {
            return LocationPersistenceConst.RouteCollection;
        }

        /// <summary>
        /// 判断指定的位置状态是否处于加锁状态（实体正在跨服传送中）。
        /// </summary>
        private static bool IsLocked(LocationTypeState state)
        {
            return state.LockToken != 0;
        }

        /// <summary>
        /// 判断指定的位置锁是否已超过过期时间（防止跨服传送异常中断导致死锁）。
        /// </summary>
        private static bool IsExpiredLock(this LocationComponent self, LocationTypeState state)
        {
            return IsLocked(state)
                   && state.LockExpireTime > 0
                   && self.GetSingleton<TimeInfo>().ServerNow() >= state.LockExpireTime;
        }

        /// <summary>
        /// 尝试从 <see cref="LocationInfo"/> 中获取指定 <paramref name="locationType"/> 的状态。
        /// </summary>
        private static bool TryGetRouteState(LocationInfo locationInfo, int locationType, out LocationTypeState state)
        {
            state = default;
            return locationInfo != null
                   && locationInfo.TypeStates != null
                   && locationInfo.TypeStates.TryGetValue(locationType, out state);
        }

        /// <summary>
        /// 创建并挂载指定 <paramref name="key"/> 的 <see cref="LocationInfo"/> 子实体。
        /// </summary>
        private static LocationInfo CreateInfo(this LocationComponent self, long key)
        {
            LocationInfo locationInfo = self.AddChildWithId<LocationInfo>(key);
            locationInfo.TypeStates ??= new Dictionary<int, LocationTypeState>();
            return locationInfo;
        }

        /// <summary>
        /// 从内存缓存中移除指定 <paramref name="key"/> 的 <see cref="LocationInfo"/> 子实体。
        /// </summary>
        private static void RemoveCachedInfo(this LocationComponent self, long key)
        {
            self.RemoveChild(key);
        }

        /// <summary>
        /// 从内存缓存中获取指定 <paramref name="key"/> 的 <see cref="LocationInfo"/> 子实体。
        /// </summary>
        private static LocationInfo GetCachedInfo(this LocationComponent self, long key)
        {
            return self.GetChild<LocationInfo>(key);
        }

        /// <summary>
        /// 创建一份 <see cref="LocationInfo.TypeStates"/> 字典快照（用于持久化失败时回滚内存状态）。
        /// </summary>
        private static Dictionary<int, LocationTypeState> CreateStatesSnapshot(LocationInfo locationInfo)
        {
            if (locationInfo == null)
            {
                return null;
            }

            return locationInfo.TypeStates == null
                    ? new Dictionary<int, LocationTypeState>()
                    : new Dictionary<int, LocationTypeState>(locationInfo.TypeStates);
        }

        /// <summary>
        /// 当数据库写入失败时，将指定 <paramref name="key"/> 的内存状态回滚到修改前的快照。
        /// </summary>
        private static void RestoreStates(this LocationComponent self, long key, Dictionary<int, LocationTypeState> snapshot)
        {
            if (snapshot == null)
            {
                self.RemoveCachedInfo(key);
                return;
            }

            LocationInfo locationInfo = self.GetCachedInfo(key) ?? self.CreateInfo(key);
            locationInfo.TypeStates.Clear();
            foreach ((int locationType, LocationTypeState state) in snapshot)
            {
                locationInfo.TypeStates[locationType] = state;
            }
        }

        /// <summary>
        /// 获取当前区服的 <see cref="DBComponent"/>。
        /// </summary>
        private static DBComponent GetDBComponent(this LocationComponent self)
        {
            Scene root = self.Root();
            DBManagerComponent dbManagerComponent = root.GetComponent<DBManagerComponent>();
            if (dbManagerComponent == null)
            {
                throw new Exception($"location db manager not found scene: {root.Name}");
            }

            return dbManagerComponent.GetZoneDB(root.Fiber.Zone);
        }

        /// <summary>
        /// 在持久化协程锁保护下将 <see cref="LocationInfo"/> 保存到 MongoDB。
        /// </summary>
        private static async ETTask SaveInfoToDB(this LocationComponent self, LocationInfo locationInfo)
        {
            if (locationInfo == null)
            {
                return;
            }

            long key = locationInfo.Id;
            EntityRef<LocationComponent> selfRef = self;
            EntityRef<LocationInfo> locationInfoRef = locationInfo;
            DBComponent dbComponent = self.GetDBComponent();
            EntityRef<DBComponent> dbComponentRef = dbComponent;
            Scene root = self.Root();
            EntityRef<Scene> rootRef = root;

            using (await root.CoroutineLockComponent.Wait(CoroutineLockType.LocationPersistence, key))
            {
                root = rootRef;
                self = selfRef;
                locationInfo = locationInfoRef;
                dbComponent = dbComponentRef;
                if (root == null || self == null || locationInfo == null || dbComponent == null)
                {
                    return;
                }

                await dbComponent.Save(locationInfo, self.GetCollectionName());
            }
        }

        /// <summary>
        /// 在持久化协程锁保护下从 MongoDB 删除指定 <paramref name="key"/> 的 <see cref="LocationInfo"/> 记录。
        /// </summary>
        private static async ETTask RemoveInfoFromDB(this LocationComponent self, long key)
        {
            EntityRef<LocationComponent> selfRef = self;
            DBComponent dbComponent = self.GetDBComponent();
            EntityRef<DBComponent> dbComponentRef = dbComponent;
            Scene root = self.Root();
            EntityRef<Scene> rootRef = root;

            using (await root.CoroutineLockComponent.Wait(CoroutineLockType.LocationPersistence, key))
            {
                root = rootRef;
                self = selfRef;
                dbComponent = dbComponentRef;
                if (root == null || self == null || dbComponent == null)
                {
                    return;
                }

                await dbComponent.Remove<LocationInfo>(key, self.GetCollectionName());
            }
        }

        /// <summary>
        /// 从 MongoDB 按需加载指定 <paramref name="key"/> 的 <see cref="LocationInfo"/> 并写入内存缓存。
        /// </summary>
        private static async ETTask<LocationInfo> LoadInfoFromDB(this LocationComponent self, long key)
        {
            EntityRef<LocationComponent> selfRef = self;
            DBComponent dbComponent = self.GetDBComponent();
            string collectionName = self.GetCollectionName();

            using LocationInfo loadedInfo = await dbComponent.Query<LocationInfo>(key, collectionName);
            self = selfRef;
            if (self == null || loadedInfo == null || loadedInfo.TypeStates == null || loadedInfo.TypeStates.Count == 0)
            {
                return null;
            }

            LocationInfo locationInfo = self.CreateInfo(key);
            foreach ((int locationType, LocationTypeState state) in loadedInfo.TypeStates)
            {
                locationInfo.TypeStates[locationType] = state;
            }

            return locationInfo;
        }

        /// <summary>
        /// 优先从内存获取 <see cref="LocationInfo"/>，若未命中则从 MongoDB 加载（支持主备切换后新主懒加载恢复路由）。
        /// </summary>
        private static async ETTask<LocationInfo> GetOrLoadInfo(this LocationComponent self, long key)
        {
            LocationInfo cached = self.GetCachedInfo(key);
            if (cached != null)
            {
                cached.TypeStates ??= new Dictionary<int, LocationTypeState>();
                return cached;
            }

            return await self.LoadInfoFromDB(key);
        }

        /// <summary>
        /// 清空当前节点内存中的所有 <see cref="LocationInfo"/> 缓存（当节点从 Primary 降级为 Backup 时调用，避免残留脏缓存）。
        /// </summary>
        public static void ClearAllCache(this LocationComponent self)
        {
            using ListComponent<long> ids = ListComponent<long>.Create();
            foreach ((long key, Entity _) in self.Children)
            {
                ids.Add(key);
            }

            foreach (long key in ids)
            {
                self.RemoveCachedInfo(key);
            }
        }

        /// <summary>
        /// 注册或更新指定实体（<paramref name="locationType"/> + <paramref name="key"/>）的 <see cref="ActorId"/> 位置映射并持久化到数据库。
        /// 若当前位置处于未过期的锁定状态，则拒绝写入并抛出 <see cref="ErrorCode.ERR_LocationAlreadyLocked"/>。
        /// </summary>
        public static async ETTask Add(this LocationComponent self, int locationType, long key, ActorId actorId)
        {
            EntityRef<LocationComponent> selfRef = self;
            using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.Location, key))
            {
                self = selfRef;
                if (self == null)
                {
                    return;
                }

                LocationInfo locationInfo = await self.GetOrLoadInfo(key);
                self = selfRef;
                if (self == null)
                {
                    return;
                }

                locationInfo ??= self.CreateInfo(key);
                Dictionary<int, LocationTypeState> snapshot = CreateStatesSnapshot(locationInfo);

                if (TryGetRouteState(locationInfo, locationType, out LocationTypeState currentState)
                    && self.IsExpiredLock(currentState))
                {
                    currentState.LockToken = default;
                    currentState.LockExpireTime = default;
                    locationInfo.TypeStates[locationType] = currentState;
                }

                if (TryGetRouteState(locationInfo, locationType, out currentState) && IsLocked(currentState))
                {
                    throw new RpcException(ErrorCode.ERR_LocationAlreadyLocked,
                        $"location add rejected by lock type: {locationType} key: {key} actorId: {currentState.ActorId} lockToken: {currentState.LockToken}");
                }

                bool oldExists = TryGetRouteState(locationInfo, locationType, out LocationTypeState oldState);
                locationInfo.TypeStates[locationType] = new LocationTypeState { ActorId = actorId };

                try
                {
                    await self.SaveInfoToDB(locationInfo);
                }
                catch
                {
                    self = selfRef;
                    if (self != null)
                    {
                        self.RestoreStates(key, snapshot);
                    }

                    throw;
                }

                if (oldExists)
                {
                    Log.Info($"location replace type: {locationType} key: {key} oldActorId: {oldState.ActorId} newActorId: {actorId}");
                    return;
                }

                Log.Info($"location add type: {locationType} key: {key} actorId: {actorId}");
            }
        }

        /// <summary>
        /// 移除指定实体（<paramref name="locationType"/> + <paramref name="key"/>）的位置映射。
        /// 若传入了 <paramref name="expectedActorId"/>，则仅当当前记录的 ActorId 与之匹配且未处于锁定态时才移除，防止旧场景延迟销毁误删新场景刚刚注册的路由。
        /// </summary>
        public static async ETTask Remove(this LocationComponent self, int locationType, long key, ActorId expectedActorId = default)
        {
            EntityRef<LocationComponent> selfRef = self;
            using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.Location, key))
            {
                self = selfRef;
                if (self == null)
                {
                    return;
                }

                LocationInfo locationInfo = await self.GetOrLoadInfo(key);
                self = selfRef;
                if (self == null || locationInfo == null)
                {
                    return;
                }

                if (!TryGetRouteState(locationInfo, locationType, out LocationTypeState state))
                {
                    return;
                }

                if (expectedActorId != default && state.ActorId != expectedActorId)
                {
                    Log.Warning(
                        $"location remove skip by actor mismatch type: {locationType} key: {key} expectedActorId: {expectedActorId} currentActorId: {state.ActorId}");
                    return;
                }

                Dictionary<int, LocationTypeState> snapshot = CreateStatesSnapshot(locationInfo);
                if (self.IsExpiredLock(state))
                {
                    state.LockToken = default;
                    state.LockExpireTime = default;
                    locationInfo.TypeStates[locationType] = state;
                }

                if (IsLocked(state))
                {
                    if (expectedActorId != default)
                    {
                        Log.Warning(
                            $"location remove skip by active lock type: {locationType} key: {key} expectedActorId: {expectedActorId} actorId: {state.ActorId} lockToken: {state.LockToken}");
                        return;
                    }

                    throw new RpcException(ErrorCode.ERR_LocationAlreadyLocked,
                        $"location remove rejected by lock type: {locationType} key: {key} actorId: {state.ActorId} lockToken: {state.LockToken}");
                }

                locationInfo.TypeStates.Remove(locationType);

                try
                {
                    if (locationInfo.TypeStates.Count == 0)
                    {
                        self.RemoveCachedInfo(key);
                        await self.RemoveInfoFromDB(key);
                    }
                    else
                    {
                        await self.SaveInfoToDB(locationInfo);
                    }
                }
                catch
                {
                    self = selfRef;
                    if (self != null)
                    {
                        self.RestoreStates(key, snapshot);
                    }

                    throw;
                }

                Log.Info($"location remove type: {locationType} key: {key}");
            }
        }

        /// <summary>
        /// 锁定指定实体的位置（在实体开始跨服传送前调用，使其他节点查询该实体位置时收到 <see cref="ErrorCode.ERR_LocationGetRetry"/> 等待解锁），并返回唯一的 <c>lockToken</c> 供解锁时校验。
        /// </summary>
        public static async ETTask<long> Lock(this LocationComponent self, int locationType, long key, ActorId actorId, int time = 0)
        {
            EntityRef<LocationComponent> selfRef = self;
            using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.Location, key))
            {
                self = selfRef;
                if (self == null)
                {
                    return default;
                }

                LocationInfo locationInfo = await self.GetOrLoadInfo(key);
                self = selfRef;
                if (self == null)
                {
                    return default;
                }

                locationInfo ??= self.CreateInfo(key);
                Dictionary<int, LocationTypeState> snapshot = CreateStatesSnapshot(locationInfo);
                bool exists = TryGetRouteState(locationInfo, locationType, out LocationTypeState state);

                if (exists && self.IsExpiredLock(state))
                {
                    state.LockToken = default;
                    state.LockExpireTime = default;
                    locationInfo.TypeStates[locationType] = state;
                }

                if (IsLocked(state))
                {
                    if (state.ActorId == actorId)
                    {
                        Log.Warning(
                            $"location lock idempotent type: {locationType} key: {key} actorId: {actorId} lockToken: {state.LockToken}");
                        return state.LockToken;
                    }

                    throw new RpcException(ErrorCode.ERR_LocationAlreadyLocked,
                        $"location lock already exists type: {locationType} key: {key} actorId: {state.ActorId} requestActorId: {actorId}");
                }

                if (exists && state.ActorId != default && state.ActorId != actorId)
                {
                    throw new RpcException(ErrorCode.ERR_LocationLockOwnerMismatch,
                        $"location lock owner mismatch type: {locationType} key: {key} requestActorId: {actorId} holdActorId: {state.ActorId}");
                }

                long lockToken = IdGenerater.Instance.GenerateId();
                long lockExpireTime = time > 0 ? self.GetSingleton<TimeInfo>().ServerNow() + time : 0;
                state.ActorId = actorId;
                state.LockToken = lockToken;
                state.LockExpireTime = lockExpireTime;
                locationInfo.TypeStates[locationType] = state;

                try
                {
                    await self.SaveInfoToDB(locationInfo);
                }
                catch
                {
                    self = selfRef;
                    if (self != null)
                    {
                        self.RestoreStates(key, snapshot);
                    }

                    throw;
                }

                Log.Info(
                    $"location lock type: {locationType} key: {key} actorId: {actorId} lockToken: {lockToken} lockExpireTime: {lockExpireTime}");

                return lockToken;
            }
        }

        /// <summary>
        /// 解锁指定实体的位置并将其路由更新为目标场景的 <paramref name="newActorId"/>（在跨服传送落地完成后调用，严格校验 <paramref name="oldActorId"/> 与 <paramref name="lockToken"/>）。
        /// </summary>
        public static async ETTask UnLock(this LocationComponent self, int locationType, long key, ActorId oldActorId, ActorId newActorId,
        long lockToken)
        {
            EntityRef<LocationComponent> selfRef = self;
            using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.Location, key))
            {
                self = selfRef;
                if (self == null)
                {
                    return;
                }

                LocationInfo locationInfo = await self.GetOrLoadInfo(key);
                self = selfRef;
                if (self == null || locationInfo == null || !TryGetRouteState(locationInfo, locationType, out LocationTypeState state))
                {
                    throw new RpcException(ErrorCode.ERR_LocationLockNotFound,
                        $"location unlock not found type: {locationType} key: {key} oldActorId: {oldActorId}");
                }

                Dictionary<int, LocationTypeState> snapshot = CreateStatesSnapshot(locationInfo);
                if (self.IsExpiredLock(state))
                {
                    state.LockToken = default;
                    state.LockExpireTime = default;
                    locationInfo.TypeStates[locationType] = state;

                    try
                    {
                        await self.SaveInfoToDB(locationInfo);
                    }
                    catch
                    {
                        self = selfRef;
                        if (self != null)
                        {
                            self.RestoreStates(key, snapshot);
                        }

                        throw;
                    }

                    throw new RpcException(ErrorCode.ERR_LocationLockNotFound,
                        $"location unlock expired type: {locationType} key: {key} oldActorId: {oldActorId}");
                }

                if (!IsLocked(state))
                {
                    throw new RpcException(ErrorCode.ERR_LocationLockNotFound,
                        $"location unlock not found type: {locationType} key: {key} oldActorId: {oldActorId}");
                }

                if (lockToken == 0 || state.LockToken != lockToken)
                {
                    throw new RpcException(ErrorCode.ERR_LocationLockTokenMismatch,
                        $"location unlock token mismatch type: {locationType} key: {key} oldActorId: {oldActorId} requestToken: {lockToken} holdToken: {state.LockToken}");
                }

                if (oldActorId != state.ActorId)
                {
                    throw new RpcException(ErrorCode.ERR_LocationLockOwnerMismatch,
                        $"location unlock old actor mismatch type: {locationType} key: {key} requestOldActorId: {oldActorId} holdActorId: {state.ActorId}");
                }

                Log.Info(
                    $"location unlock type: {locationType} key: {key} actorId: {oldActorId} newActorId: {newActorId} lockToken: {lockToken}");

                state.ActorId = newActorId;
                state.LockToken = default;
                state.LockExpireTime = default;
                locationInfo.TypeStates[locationType] = state;

                try
                {
                    await self.SaveInfoToDB(locationInfo);
                }
                catch
                {
                    self = selfRef;
                    if (self != null)
                    {
                        self.RestoreStates(key, snapshot);
                    }

                    throw;
                }
            }
        }

        /// <summary>
        /// 查询指定实体的当前 <see cref="ActorId"/>。
        /// 若实体当前处于未过期的位置锁定状态（正在跨服迁移中），则抛出 <see cref="ErrorCode.ERR_LocationGetRetry"/> 通知调用方稍后重试。
        /// </summary>
        public static async ETTask<ActorId> Get(this LocationComponent self, int locationType, long key)
        {
            EntityRef<LocationComponent> selfRef = self;
            using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.Location, key))
            {
                self = selfRef;
                if (self == null)
                {
                    return default;
                }

                LocationInfo locationInfo = await self.GetOrLoadInfo(key);
                self = selfRef;
                if (self == null || locationInfo == null)
                {
                    return default;
                }

                if (!TryGetRouteState(locationInfo, locationType, out LocationTypeState state))
                {
                    return default;
                }

                if (self.IsExpiredLock(state))
                {
                    Dictionary<int, LocationTypeState> snapshot = CreateStatesSnapshot(locationInfo);
                    state.LockToken = default;
                    state.LockExpireTime = default;
                    locationInfo.TypeStates[locationType] = state;

                    try
                    {
                        await self.SaveInfoToDB(locationInfo);
                    }
                    catch
                    {
                        self = selfRef;
                        if (self != null)
                        {
                            self.RestoreStates(key, snapshot);
                        }

                        throw;
                    }
                }
                else if (IsLocked(state))
                {
                    throw new RpcException(ErrorCode.ERR_LocationGetRetry,
                        $"location get retry type: {locationType} key: {key} actorId: {state.ActorId} lockToken: {state.LockToken}");
                }

                Log.Info($"location get type: {locationType} key: {key} actorId: {state.ActorId}");
                return state.ActorId;
            }
        }

        /// <summary>
        /// 根据服务发现中所有在线的 Location 节点列表，按最小 PriorityId 稳定排序规则刷新本节点的主备角色（若从主降为备则立即清空内存缓存）。
        /// </summary>
        public static void RefreshPrimaryState(this LocationComponent self)
        {
            Scene root = self.Root();
            if (root == null)
            {
                return;
            }

            ServiceDiscoveryProxy serviceDiscoveryProxy = root.GetComponent<ServiceDiscoveryProxy>();
            if (serviceDiscoveryProxy == null)
            {
                return;
            }

            List<ServiceInfo> serviceInfos = serviceDiscoveryProxy.GetBySceneType(SceneType.Location);
            string primarySceneName = string.Empty;
            bool isPrimaryLocation = false;
            if (serviceInfos.Count > 0)
            {
                primarySceneName = LocationPrimaryHelper.SelectStablePrimarySceneName(serviceInfos);
                isPrimaryLocation = string.Equals(root.Name, primarySceneName, StringComparison.Ordinal);
            }

            bool oldIsPrimaryLocation = self.IsPrimaryLocation;
            string oldPrimarySceneName = self.PrimaryLocationSceneName;
            self.IsPrimaryLocation = isPrimaryLocation;
            self.PrimaryLocationSceneName = primarySceneName;

            if (oldIsPrimaryLocation && !isPrimaryLocation)
            {
                self.ClearAllCache();
                Log.Warning($"location component demoted to backup scene: {root.Name} primary: {primarySceneName}");
                return;
            }

            if (oldIsPrimaryLocation != isPrimaryLocation || !string.Equals(oldPrimarySceneName, primarySceneName, StringComparison.Ordinal))
            {
                Log.Info($"location component primary state changed scene: {root.Name} isPrimary: {isPrimaryLocation} primary: {primarySceneName}");
            }
        }

        /// <summary>
        /// 校验当前 Location 节点是否为主节点（Primary）；若为备节点则在响应中设置 <see cref="ErrorCode.ERR_LocationFollowerRejected"/> 拒绝请求。
        /// </summary>
        public static bool EnsurePrimary(this LocationComponent self, IResponse response)
        {
            if (response == null)
            {
                return false;
            }

            if (self != null && self.IsPrimaryLocation)
            {
                return true;
            }

            if (self != null && !string.IsNullOrEmpty(self.PrimaryLocationSceneName))
            {
                Scene root = self.Root();
                if (root != null)
                {
                    ServiceDiscoveryProxy serviceDiscoveryProxy = root.GetComponent<ServiceDiscoveryProxy>();
                    if (serviceDiscoveryProxy != null)
                    {
                        try
                        {
                            ServiceInfo primaryServiceInfo = serviceDiscoveryProxy.GetServiceInfo(self.PrimaryLocationSceneName);
                            if (primaryServiceInfo != null && primaryServiceInfo.ActorId == root.GetActorId())
                            {
                                return true;
                            }
                        }
                        catch
                        {
                        }
                    }
                }

                response.Error = ErrorCode.ERR_LocationFollowerRejected;
                response.Message = $"location is follower currentPrimary: {self.PrimaryLocationSceneName}";
                return false;
            }

            response.Error = ErrorCode.ERR_LocationPrimaryUnavailable;
            response.Message = "location primary unavailable";
            return false;
        }
    }
}

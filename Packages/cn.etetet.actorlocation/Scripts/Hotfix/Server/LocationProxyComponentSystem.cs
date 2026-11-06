using System;
using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 业务场景访问位置服务（Location）的客户端代理组件系统（<see cref="LocationProxyComponent"/>）。
    /// 负责通过 <see cref="ServiceDiscoveryProxy"/> 动态定位当前 Primary Location 节点，封装 Add / Remove / LockWithToken / UnLockWithRetry / Get 操作，并内置主备切换重试与位置锁等待重试。
    /// </summary>
    [EntitySystemOf(typeof(LocationProxyComponent))]
    public static partial class LocationProxyComponentSystem
    {
        /// <summary>
        /// 监听服务下线事件，若下线的是当前缓存的 Primary Location 节点，则立即清空本地主节点缓存以便下次请求重新选择新主。
        /// </summary>
        [Event(SceneType.All)]
        public class OnServiceChangeRemoveService_ClearLocationPrimary: AEvent<Scene, OnServiceChangeRemoveService>
        {
            protected override async ETTask Run(Scene scene, OnServiceChangeRemoveService args)
            {
                LocationProxyComponent locationProxyComponent = scene.GetComponent<LocationProxyComponent>();
                if (locationProxyComponent == null)
                {
                    await ETTask.CompletedTask;
                    return;
                }

                if (locationProxyComponent.primaryLocationSceneName != args.ServiceName)
                {
                    await ETTask.CompletedTask;
                    return;
                }

                try
                {
                    StartSceneConfigCategory startSceneConfigCategory = scene.Fiber().GetSingleton<StartSceneConfigCategory>();
                    StartSceneConfig startSceneConfig = startSceneConfigCategory.GetBySceneName(args.ServiceName);
                    string locationSceneType = SceneTypeSingleton.Instance.GetSceneName(SceneType.Location);
                    if (startSceneConfig.SceneType != locationSceneType)
                    {
                        await ETTask.CompletedTask;
                        return;
                    }
                }
                catch
                {
                    await ETTask.CompletedTask;
                    return;
                }

                string oldPrimarySceneName = locationProxyComponent.primaryLocationSceneName;
                ulong oldPriorityId = locationProxyComponent.primaryLocationPriorityId;
                locationProxyComponent.primaryLocationSceneName = string.Empty;
                locationProxyComponent.primaryLocationPriorityId = 0;
                Log.Warning(
                    $"location proxy primary removed by service change: {oldPrimarySceneName} priorityId: {oldPriorityId}, scene: {scene.Name}");

                await ETTask.CompletedTask;
            }
        }

        [EntitySystem]
        private static void Awake(this LocationProxyComponent self)
        {
            self.primaryLocationPriorityId = 0;
            self.locationRequestRetryTimes = 20;
            self.locationRequestRetryIntervalMs = 100;
        }

        /// <summary>
        /// 从本地服务发现缓存中按稳定排序规则选出当前 Primary Location 节点的场景名称。
        /// </summary>
        private static string GetPrimaryLocationSceneName(this LocationProxyComponent self)
        {
            ServiceDiscoveryProxy serviceDiscoveryProxy = self.Root().GetComponent<ServiceDiscoveryProxy>();
            List<ServiceInfo> serviceInfos = serviceDiscoveryProxy.GetBySceneType(SceneType.Location);
            if (serviceInfos.Count == 0)
            {
                throw new RpcException(ErrorCode.ERR_LocationPrimaryUnavailable, $"not found location scene in zone: {self.Zone()}");
            }

            ServiceInfo stablePrimaryServiceInfo = LocationPrimaryHelper.SelectStablePrimaryServiceInfo(serviceInfos);
            string stablePrimarySceneName = stablePrimaryServiceInfo.SceneName;
            ulong stablePrimaryPriorityId = LocationPrimaryHelper.GetPriorityId(stablePrimaryServiceInfo);
            string primarySceneName = self.primaryLocationSceneName;
            ulong primaryPriorityId = self.primaryLocationPriorityId;
            if (primarySceneName == stablePrimarySceneName && primaryPriorityId == stablePrimaryPriorityId)
            {
                return stablePrimarySceneName;
            }

            self.primaryLocationSceneName = stablePrimarySceneName;
            self.primaryLocationPriorityId = stablePrimaryPriorityId;
            Log.Info(
                $"location proxy primary switch: {primarySceneName}({primaryPriorityId}) -> {stablePrimarySceneName}({stablePrimaryPriorityId}) scene: {self.Root().Name}");
            return stablePrimarySceneName;
        }

        /// <summary>
        /// 清空本地缓存的 Primary Location 节点记录。
        /// </summary>
        private static void ClearPrimary(this LocationProxyComponent self, string reason)
        {
            if (self == null)
            {
                return;
            }

            string oldPrimarySceneName = self.primaryLocationSceneName;
            ulong oldPriorityId = self.primaryLocationPriorityId;
            self.primaryLocationSceneName = string.Empty;
            self.primaryLocationPriorityId = 0;
            Log.Warning(
                $"location proxy clear primary reason: {reason} old: {oldPrimarySceneName}({oldPriorityId}) scene: {self.Root().Name}");
        }

        /// <summary>
        /// 向 Primary Location 节点发送请求，遇到 Follower 拒绝或主节点不可用时自动清空缓存并等待重试。
        /// </summary>
        private static async ETTask<IResponse> CallPrimaryWithRetry(this LocationProxyComponent self, long key, IRequest request)
        {
            EntityRef<LocationProxyComponent> selfRef = self;
            int retryCount = 0;
            while (true)
            {
                self = selfRef;
                if (self == null)
                {
                    throw new Exception("location proxy disposed");
                }

                string primarySceneName = string.Empty;
                ulong primaryPriorityId = 0;
                try
                {
                    primarySceneName = self.GetPrimaryLocationSceneName();
                    primaryPriorityId = self.primaryLocationPriorityId;
                    ServiceDiscoveryProxy serviceDiscoveryProxy = self.Root().GetComponent<ServiceDiscoveryProxy>();
                    ActorId primaryActorId = serviceDiscoveryProxy.GetServiceInfo(primarySceneName).ActorId;
                    if (primaryActorId == default)
                    {
                        throw new Exception($"location primary actor id is empty: {primarySceneName}");
                    }

                    MessageSender messageSender = self.Root().GetComponent<MessageSender>();
                    if (messageSender == null)
                    {
                        throw new Exception($"location proxy message sender is null: {self.Root().Name}");
                    }

                    IResponse response = await messageSender.Call(primaryActorId, request);
                    if (response.Error == ErrorCode.ERR_LocationFollowerRejected
                        || response.Error == ErrorCode.ERR_LocationPrimaryUnavailable)
                    {
                        ++retryCount;
                        self = selfRef;
                        if (self == null)
                        {
                            return response;
                        }

                        self.ClearPrimary($"response-error:{response.Error}");
                        Log.Warning(
                            $"location proxy retry after primary rejection key: {key} scene: {primarySceneName} priorityId: {primaryPriorityId} retry: {retryCount}/{self.locationRequestRetryTimes} error: {response.Error} message: {response.Message}");

                        if (retryCount >= self.locationRequestRetryTimes)
                        {
                            return response;
                        }

                        Scene root = self.Root();
                        EntityRef<Scene> rootRef = root;
                        await root.TimerComponent.WaitAsync(self.locationRequestRetryIntervalMs);

                        root = rootRef;
                        if (root == null)
                        {
                            throw new Exception("location proxy root disposed");
                        }

                        continue;
                    }

                    return response;
                }
                catch (Exception e)
                {
                    ++retryCount;
                    self = selfRef;
                    if (self == null)
                    {
                        throw;
                    }

                    if (e is RpcException rpcException && rpcException.Error == ErrorCode.ERR_LocationPrimaryUnavailable)
                    {
                        self.ClearPrimary($"select-primary:{rpcException.Error}");
                    }

                    Log.Warning(
                        $"location proxy call failed key: {key} scene: {primarySceneName} priorityId: {primaryPriorityId} retry: {retryCount}/{self.locationRequestRetryTimes} error: {e.Message}");

                    if (retryCount >= self.locationRequestRetryTimes)
                    {
                        throw;
                    }

                    Scene root = self.Root();
                    EntityRef<Scene> rootRef = root;
                    await root.TimerComponent.WaitAsync(self.locationRequestRetryIntervalMs);

                    root = rootRef;
                    if (root == null)
                    {
                        throw new Exception("location proxy root disposed");
                    }
                }
            }
        }

        /// <summary>
        /// 向 Location 服务注册实体位置映射。
        /// </summary>
        public static async ETTask Add(this LocationProxyComponent self, int type, long key, ActorId actorId)
        {
            Log.Info($"location proxy add {key}, {actorId} {self.GetSingleton<TimeInfo>().ServerNow()}");
            ObjectAddRequest request = ObjectAddRequest.Create();
            request.Type = type;
            request.Key = key;
            request.ActorId = actorId;

            ObjectAddResponse response = (ObjectAddResponse)await self.CallPrimaryWithRetry(key, request);
            if (response.Error != ErrorCode.ERR_Success)
            {
                throw new RpcException(response.Error,
                    $"location add failed key: {key} actorId: {actorId} error: {response.Message}");
            }
        }

        /// <summary>
        /// 锁定实体位置并返回唯一解锁令牌（<c>LockToken</c>）。
        /// </summary>
        public static async ETTask<long> LockWithToken(this LocationProxyComponent self, int type, long key, ActorId actorId,
            int time = 60000)
        {
            Log.Info($"location proxy lock {key}, {actorId} {self.GetSingleton<TimeInfo>().ServerNow()}");

            ObjectLockRequest request = ObjectLockRequest.Create();
            request.Type = type;
            request.Key = key;
            request.ActorId = actorId;
            request.Time = time;

            ObjectLockResponse response = (ObjectLockResponse)await self.CallPrimaryWithRetry(key, request);
            if (response.Error != ErrorCode.ERR_Success)
            {
                throw new RpcException(response.Error,
                    $"location lock failed key: {key} actorId: {actorId} error: {response.Message}");
            }

            return response.LockToken;
        }

        [Obsolete("Use LockWithToken and pass the returned token to UnLock.", true)]
        public static async ETTask Lock(this LocationProxyComponent self, int type, long key, ActorId actorId,
            int time = 60000)
        {
            await self.LockWithToken(type, key, actorId, time);
        }

        [Obsolete("Use UnLock overload with lockToken.", true)]
        public static ETTask UnLock(this LocationProxyComponent self, int type, long key, ActorId oldActorId,
            ActorId newActorId)
        {
            throw new RpcException(ErrorCode.ERR_LocationLockTokenMismatch,
                $"location unlock requires lock token key: {key} oldActorId: {oldActorId} newActorId: {newActorId}");
        }

        /// <summary>
        /// 携带 <paramref name="lockToken"/> 解锁实体位置并更新为 <paramref name="newActorId"/>。
        /// </summary>
        public static async ETTask UnLock(this LocationProxyComponent self, int type, long key, ActorId oldActorId,
            ActorId newActorId, long lockToken)
        {
            Log.Info(
                $"location proxy unlock {key}, {newActorId} lockToken: {lockToken} {self.GetSingleton<TimeInfo>().ServerNow()}");

            ObjectUnLockRequest request = ObjectUnLockRequest.Create();
            request.Type = type;
            request.Key = key;
            request.OldActorId = oldActorId;
            request.NewActorId = newActorId;
            request.LockToken = lockToken;

            ObjectUnLockResponse response = (ObjectUnLockResponse)await self.CallPrimaryWithRetry(key, request);
            if (response.Error != ErrorCode.ERR_Success)
            {
                throw new RpcException(response.Error,
                    $"location unlock failed key: {key} oldActorId: {oldActorId} newActorId: {newActorId} lockToken: {lockToken} error: {response.Message}");
            }
        }

        /// <summary>
        /// 带幂等补偿检查的解锁方法：当解锁因网络超时重发遇到锁不存在或 Token 不匹配时，会主动查询一次当前位置，若已更新为 <paramref name="newActorId"/> 则视为解锁成功。
        /// </summary>
        public static async ETTask UnLockWithRetry(this LocationProxyComponent self, int type, long key, ActorId oldActorId,
            ActorId newActorId, long lockToken)
        {
            EntityRef<LocationProxyComponent> selfRef = self;
            int retryCount = 0;
            while (true)
            {
                self = selfRef;
                if (self == null)
                {
                    throw new Exception("location proxy disposed");
                }

                try
                {
                    await self.UnLock(type, key, oldActorId, newActorId, lockToken);
                    return;
                }
                catch (RpcException e) when (e.Error == ErrorCode.ERR_LocationLockNotFound
                                             || e.Error == ErrorCode.ERR_LocationLockOwnerMismatch
                                             || e.Error == ErrorCode.ERR_LocationLockTokenMismatch)
                {
                    LocationProxyComponent selfForGet = selfRef;
                    if (selfForGet == null)
                    {
                        throw new Exception("location proxy disposed");
                    }
                    ActorId actorId = await selfForGet.Get(type, key);

                    self = selfRef;
                    if (self == null)
                    {
                        throw new Exception("location proxy disposed");
                    }

                    if (actorId == newActorId)
                    {
                        Log.Warning(
                            $"location proxy unlock compensate success key: {key} actorId: {actorId} lockToken: {lockToken} reason: {e.Error}");
                        return;
                    }

                    ++retryCount;
                    Log.Warning(
                        $"location proxy unlock compensate retry key: {key} actorId: {actorId} target: {newActorId} retry: {retryCount}/{self.locationRequestRetryTimes} lockToken: {lockToken} reason: {e.Error}");
                    if (retryCount >= self.locationRequestRetryTimes)
                    {
                        throw;
                    }

                    Scene root = self.Root();
                    EntityRef<Scene> rootRef = root;
                    await root.TimerComponent.WaitAsync(self.locationRequestRetryIntervalMs * retryCount);
                    root = rootRef;
                    if (root == null)
                    {
                        throw new Exception("location proxy root disposed");
                    }
                }
            }
        }

        /// <summary>
        /// 从 Location 服务移除指定实体的位置映射（不校验期望 ActorId）。
        /// </summary>
        public static async ETTask Remove(this LocationProxyComponent self, int type, long key)
        {
            await self.Remove(type, key, default);
        }

        /// <summary>
        /// 从 Location 服务移除指定实体的位置映射（校验当前持有者是否为 <paramref name="expectedActorId"/>）。
        /// </summary>
        public static async ETTask Remove(this LocationProxyComponent self, int type, long key, ActorId expectedActorId)
        {
            Log.Info($"location proxy remove {key}, {self.GetSingleton<TimeInfo>().ServerNow()}");

            ObjectRemoveRequest request = ObjectRemoveRequest.Create();
            request.Type = type;
            request.Key = key;
            request.ExpectedActorId = expectedActorId;

            ObjectRemoveResponse response = (ObjectRemoveResponse)await self.CallPrimaryWithRetry(key, request);
            if (response.Error != ErrorCode.ERR_Success)
            {
                throw new RpcException(response.Error,
                    $"location remove failed key: {key} expectedActorId: {expectedActorId} error: {response.Message}");
            }
        }

        /// <summary>
        /// 查询指定实体的当前 <see cref="ActorId"/>（若目标正处于位置锁定迁移中，则自动等待并重试）。
        /// </summary>
        public static async ETTask<ActorId> Get(this LocationProxyComponent self, int type, long key)
        {
            if (key == 0)
            {
                throw new Exception("get location key 0");
            }

            EntityRef<LocationProxyComponent> selfRef = self;
            int retryCount = 0;
            while (true)
            {
                self = selfRef;
                if (self == null)
                {
                    throw new Exception("location proxy disposed");
                }

                ObjectGetRequest request = ObjectGetRequest.Create();
                request.Type = type;
                request.Key = key;

                ObjectGetResponse response = (ObjectGetResponse)await self.CallPrimaryWithRetry(key, request);
                if (response.Error == ErrorCode.ERR_Success)
                {
                    return response.ActorId;
                }

                if (response.Error == ErrorCode.ERR_LocationGetRetry)
                {
                    ++retryCount;
                    self = selfRef;
                    if (self == null)
                    {
                        throw new Exception("location proxy disposed");
                    }

                    if (retryCount >= self.locationRequestRetryTimes)
                    {
                        throw new RpcException(response.Error,
                            $"location get retry exceeded key: {key} retry: {retryCount}/{self.locationRequestRetryTimes}");
                    }

                    int delayMs = self.locationRequestRetryIntervalMs * retryCount;
                    if (delayMs <= 0)
                    {
                        delayMs = 1;
                    }

                    Scene root = self.Root();
                    EntityRef<Scene> rootRef = root;
                    await root.TimerComponent.WaitAsync(delayMs);
                    root = rootRef;
                    if (root == null)
                    {
                        throw new Exception("location proxy root disposed");
                    }
                    continue;
                }

                throw new RpcException(response.Error,
                    $"location get failed key: {key} error: {response.Error} message: {response.Message}");
            }
        }

        /// <summary>
        /// 将当前实体自身（<c>self.Id</c> -> <c>self.GetActorId()</c>）注册到指定 <paramref name="type"/> 的 Location 表中。
        /// </summary>
        public static async ETTask AddLocation(this Entity self, int type)
        {
            await self.Root().GetComponent<LocationProxyComponent>().Add(type, self.Id, self.GetActorId());
        }

        /// <summary>
        /// 将当前实体自身从指定 <paramref name="type"/> 的 Location 表中移除（自动携带自身 ActorId 进行期望值保护）。
        /// </summary>
        public static async ETTask RemoveLocation(this Entity self, int type)
        {
            await self.Root().GetComponent<LocationProxyComponent>().Remove(type, self.Id, self.GetActorId());
        }
    }
}

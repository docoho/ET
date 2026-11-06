using System;
using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 服务发现代理组件系统（挂载于各业务 Scene 上，供业务层注册自身服务、订阅其他服务变更以及按路由键调用目标服务）。
    /// </summary>
    [EntitySystemOf(typeof(ServiceDiscoveryProxy))]
    public static partial class ServiceDiscoveryProxySystem
    {
        /// <summary>解析目标服务 ActorId 时的默认最大重试次数</summary>
        private const int DefaultServiceResolveRetryTimes = 20;
        /// <summary>解析目标服务重试时的默认等待间隔（毫秒）</summary>
        private const int DefaultServiceResolveRetryIntervalMs = 100;
        /// <summary>服务变更类型：新增或更新</summary>
        private const int ServiceChangeTypeAdd = 1;
        /// <summary>服务变更类型：移除</summary>
        private const int ServiceChangeTypeRemove = 2;
        /// <summary>服务变更来源标识：初始订阅响应快照</summary>
        private const string ServiceChangeSourceSubscribeSnapshot = "subscribe-response";
        /// <summary>服务变更来源标识：进程 Agent 实时推送</summary>
        private const string ServiceChangeSourceAgentPush = "agent-notification";

        /// <summary>
        /// 当业务 Scene 销毁时，向同进程 <see cref="ServiceDiscoveryAgent"/> 发送单向消息通知注销本场景服务并移除订阅。
        /// </summary>
        [EntitySystem]
        private static void Destroy(this ServiceDiscoveryProxy self)
        {
            if (self.AgentFiberInstanceId == default)
            {
                return;
            }

            Scene scene = self.Scene();
            if (scene == null)
            {
                return;
            }

            ServiceProxyDestroyUnregisterMessage message = ServiceProxyDestroyUnregisterMessage.Create();
            message.SceneName = self.RootName;
            MessageQueue.Instance.Send(scene.Fiber, self.AgentFiberInstanceId, message);
        }

        /// <summary>
        /// 组件唤醒时校验场景类型、定位本进程的 <see cref="SceneType.ServiceDiscoveryAgent"/> FiberInstanceId，并启动 Proxy 心跳组件。
        /// </summary>
        [EntitySystem]
        private static void Awake(this ServiceDiscoveryProxy self)
        {
            Scene root = self.Root();
            if (root.SceneType == SceneType.ServiceDiscoveryAgent)
            {
                throw new Exception("service discovery proxy should not be added to ServiceDiscoveryAgent scene");
            }

            self.RootName = root.Name;

            self.ProcessInnerSender = root.GetComponent<ProcessInnerSender>();
            if (self.ProcessInnerSender == null)
            {
                throw new Exception($"service discovery proxy process inner sender is null, scene: {root.Name}");
            }

            ProcessFiberAddressSingleton processFiberAddressSingleton = self.Fiber().GetSingleton<ProcessFiberAddressSingleton>();
            if (processFiberAddressSingleton == null)
            {
                throw new Exception($"process fiber address singleton is null, scene: {root.Name}");
            }

            self.AgentFiberInstanceId = processFiberAddressSingleton.Get(SceneType.ServiceDiscoveryAgent);
            if (self.AgentFiberInstanceId == default)
            {
                throw new Exception($"service discovery agent fiber instance id is empty, scene: {root.Name}");
            }

            self.ServiceResolveRetryTimes = DefaultServiceResolveRetryTimes;
            self.ServiceResolveRetryIntervalMs = DefaultServiceResolveRetryIntervalMs;
            self.AddComponent<ServiceDiscoveryProxyHeartbeat>();
        }

        /// <summary>
        /// 注册到服务发现服务器（自动附带当前场景的 <see cref="ServiceMetaKey.SceneType"/> 和 <see cref="ServiceMetaKey.Zone"/> 元数据）。
        /// </summary>
        public static async ETTask RegisterToServiceDiscovery(this ServiceDiscoveryProxy self, StringKV metadata = null)
        {
            metadata ??= new StringKV();

            if (self.AgentFiberInstanceId == default)
            {
                throw new Exception($"service discovery agent fiber instance id is empty, scene: {self.Root().Name}");
            }

            EntityRef<ServiceDiscoveryProxy> selfRef = self;
            Scene root = self.Root();
            string sceneTypeName = SceneTypeSingleton.Instance.GetSceneName(self.Scene().SceneType);
            string sceneName = root.Name;
            ActorId actorId = root.GetActorId();
            if (!ServiceDiscoveryHelper.TryValidateRequiredText(sceneTypeName, nameof(ServiceRegisterRequest), "SceneType",
                    out string errorMessage))
            {
                throw new ArgumentException(errorMessage);
            }

            if (!ServiceDiscoveryHelper.TryValidateRequiredText(sceneName, nameof(ServiceRegisterRequest), nameof(ServiceRegisterRequest.SceneName),
                    out errorMessage))
            {
                throw new ArgumentException(errorMessage);
            }

            if (!ServiceDiscoveryHelper.TryValidateRequiredActorId(actorId, nameof(ServiceRegisterRequest), nameof(ServiceRegisterRequest.ActorId),
                    out errorMessage))
            {
                throw new ArgumentException(errorMessage);
            }

            ServiceRegisterRequest request = ServiceRegisterRequest.Create();
            request.SceneName = sceneName;
            request.ActorId = actorId;
            request.Metadata.Add(ServiceMetaKey.SceneType, sceneTypeName);
            request.Metadata.Add(ServiceMetaKey.Zone, root.Fiber.Zone.ToString());

            if (!ServiceDiscoveryHelper.TryValidateMetadataMap(metadata, nameof(ServiceRegisterRequest), nameof(ServiceRegisterRequest.Metadata),
                    out errorMessage))
            {
                throw new ArgumentException(errorMessage);
            }

            foreach ((string key, string value) in metadata)
            {
                request.Metadata.Add(key, value);
            }

            using ServiceRegisterResponse _ = await self.ForwardToDiscoveryByFailover<ServiceRegisterResponse>(request,
                nameof(ServiceRegisterRequest));
            self = selfRef;
            if (self == null)
            {
                return;
            }

            FiberInstanceId target = self.AgentFiberInstanceId;
            Log.Debug($"Service registered successfully scene: {sceneName} target: {target}");
        }

        /// <summary>
        /// 从服务发现服务器注销
        /// </summary>
        public static async ETTask UnregisterFromServiceDiscovery(this ServiceDiscoveryProxy self)
        {
            Scene root = self.Root();
            if (!ServiceDiscoveryHelper.TryValidateRequiredText(root.Name, nameof(ServiceUnregisterRequest), nameof(ServiceUnregisterRequest.SceneName),
                    out string errorMessage))
            {
                throw new ArgumentException(errorMessage);
            }

            ServiceUnregisterRequest request = ServiceUnregisterRequest.Create();
            request.SceneName = root.Name;
            using ServiceUnregisterResponse _ = await self.ForwardToDiscoveryByFailover<ServiceUnregisterResponse>(request,
                nameof(ServiceUnregisterRequest));
        }

        /// <summary>
        /// 订阅服务变更（向同进程 Agent 注册过滤条件，并将返回的初始快照写入本地缓存）。
        /// </summary>
        public static async ETTask SubscribeServiceChange(this ServiceDiscoveryProxy self, string filterName, StringKV filterMeta)
        {
            filterMeta ??= new StringKV();
            if (!ServiceDiscoveryHelper.TryValidateRequiredText(filterName, nameof(ServiceSubscribeRequest), nameof(ServiceSubscribeRequest.FilterName),
                    out string errorMessage))
            {
                throw new ArgumentException(errorMessage);
            }

            EntityRef<ServiceDiscoveryProxy> selfRef = self;
            self.SubscribeFilters[filterName] = ServiceDiscoveryHelper.CloneMetadata(filterMeta);

            Scene root = self.Root();
            if (!ServiceDiscoveryHelper.TryValidateRequiredText(root.Name, nameof(ServiceSubscribeRequest), nameof(ServiceSubscribeRequest.SceneName),
                    out errorMessage))
            {
                throw new ArgumentException(errorMessage);
            }

            ActorId subscriberActorId = root.GetActorId();
            if (!ServiceDiscoveryHelper.TryValidateRequiredActorId(subscriberActorId, nameof(ServiceSubscribeRequest),
                    nameof(ServiceSubscribeRequest.SubscriberActorId), out errorMessage))
            {
                throw new ArgumentException(errorMessage);
            }

            ServiceSubscribeRequest request = ServiceSubscribeRequest.Create();
            request.SceneName = root.Name;
            request.SubscriberActorId = subscriberActorId;
            request.FilterName = filterName;
            if (!ServiceDiscoveryHelper.TryValidateMetadataMap(filterMeta, nameof(ServiceSubscribeRequest),
                    nameof(ServiceSubscribeRequest.FilterMetadata), out errorMessage))
            {
                throw new ArgumentException(errorMessage);
            }

            foreach ((string key, string value) in filterMeta)
            {
                request.FilterMetadata.Add(key, value);
            }

            using ServiceSubscribeResponse response = await self.ForwardToDiscoveryByFailover<ServiceSubscribeResponse>(request,
                nameof(ServiceSubscribeRequest));
            self = selfRef;
            if (self == null)
            {
                return;
            }

            Log.Info(
                $"ServiceDiscoveryProxy subscribe response proxy: {self.RootName} filterName: {filterName} filterMetadata: {FormatMetadata(filterMeta)} serviceCount: {response.Services?.Count ?? 0}");
            self.OnServiceChangeNotification(ServiceChangeTypeAdd, response.Services, ServiceChangeSourceSubscribeSnapshot);
        }

        /// <summary>
        /// 取消订阅服务变更（当前场景所有订阅）
        /// </summary>
        public static async ETTask UnsubscribeServiceChange(this ServiceDiscoveryProxy self)
        {
            EntityRef<ServiceDiscoveryProxy> selfRef = self;
            Scene root = self.Root();
            if (!ServiceDiscoveryHelper.TryValidateRequiredText(root.Name, nameof(ServiceUnsubscribeRequest), nameof(ServiceUnsubscribeRequest.SceneName),
                    out string errorMessage))
            {
                throw new ArgumentException(errorMessage);
            }

            ServiceUnsubscribeRequest request = ServiceUnsubscribeRequest.Create();
            request.SceneName = root.Name;
            using ServiceUnsubscribeResponse _ = await self.ForwardToDiscoveryByFailover<ServiceUnsubscribeResponse>(request,
                nameof(ServiceUnsubscribeRequest));

            self = selfRef;
            if (self == null)
            {
                return;
            }

            self.SubscribeFilters.Clear();
            self.ClearLocalServices(false);
        }

        /// <summary>
        /// 通过进程内通信（<see cref="ProcessInnerSender"/>）将请求转发给同进程的 <see cref="ServiceDiscoveryAgent"/>。
        /// </summary>
        public static async ETTask<T> ForwardToDiscoveryByFailover<T>(this ServiceDiscoveryProxy self, IRequest request,
            string requestName)
            where T : class, IResponse
        {
            if (self == null)
            {
                throw new Exception("service discovery proxy disposed");
            }

            if (self.AgentFiberInstanceId == default)
            {
                throw new Exception($"service discovery agent fiber instance id is empty, request: {requestName}");
            }

            if (request == null)
            {
                throw new ArgumentException($"{requestName} invalid: request is null.");
            }

            IResponse rawResponse = await self.ProcessInnerSender.Call(self.AgentFiberInstanceId, request);
            if (rawResponse is not T response)
            {
                (rawResponse as MessageObject)?.Dispose();
                throw new Exception(
                    $"service discovery agent response type mismatch, request: {requestName}, actual: {rawResponse?.GetType().Name}");
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
                    $"service discovery agent response error, request: {requestName}, error: {error}, message: {message}");
            }

            return response;
        }

        /// <summary>
        /// 根据目标 <see cref="SceneType"/> 与路由键（<paramref name="routeKey"/>）解析目标服务的 <see cref="ActorId"/>（若本地缓存暂未同步到位则自动等待重试）。
        /// </summary>
        public static async ETTask<ActorId> ResolveServiceActorIdByRouteKeyAsync(this ServiceDiscoveryProxy self, int sceneType,
            long routeKey = 0)
        {
            EntityRef<ServiceDiscoveryProxy> selfRef = self;
            string sceneTypeName = SceneTypeSingleton.Instance.GetSceneName(sceneType);
            if (string.IsNullOrWhiteSpace(sceneTypeName))
            {
                throw new ArgumentException($"resolve service invalid sceneType: {sceneType}");
            }

            int retryCount = 0;
            while (true)
            {
                self = selfRef;
                if (self == null)
                {
                    throw new Exception("service discovery proxy disposed");
                }

                try
                {
                    return self.ResolveServiceActorIdByRouteKeyOnce(sceneType, sceneTypeName, routeKey);
                }
                catch (RpcException rpcException)
                {
                    ++retryCount;
                    self = selfRef;
                    if (self == null)
                    {
                        throw new Exception("service discovery proxy disposed");
                    }

                    if (!self.ShouldRetryResolveService(rpcException) || retryCount >= self.ServiceResolveRetryTimes)
                    {
                        throw;
                    }

                    await self.WaitResolveRetryDelayAsync();
                }
            }
        }

        /// <summary>
        /// 根据目标 <see cref="SceneType"/> 与路由键（<paramref name="routeKey"/>）定位服务并发起 RPC 调用（支持自动重试可恢复的路由/网络错误）。
        /// </summary>
        public static async ETTask<T> CallBySceneTypeAsync<T>(this ServiceDiscoveryProxy self, int sceneType, IRequest request,
            string requestName, long routeKey = 0, bool needException = false, bool throwResponseError = false) where T : class, IResponse
        {
            if (request == null)
            {
                throw new ArgumentException($"{requestName} invalid: request is null.");
            }

            string sceneTypeName = SceneTypeSingleton.Instance.GetSceneName(sceneType);
            if (string.IsNullOrWhiteSpace(sceneTypeName))
            {
                throw new ArgumentException($"{requestName} invalid: sceneType is unknown: {sceneType}");
            }

            EntityRef<ServiceDiscoveryProxy> selfRef = self;
            int retryCount = 0;
            while (true)
            {
                self = selfRef;
                if (self == null)
                {
                    throw new Exception("service discovery proxy disposed");
                }

                try
                {
                    ActorId actorId = self.ResolveServiceActorIdByRouteKeyOnce(sceneType, sceneTypeName, routeKey);

                    MessageSender messageSender = self.Root().GetComponent<MessageSender>();
                    if (messageSender == null)
                    {
                        throw new Exception($"service discovery proxy message sender is null, request: {requestName} scene: {self.Root().Name}");
                    }

                    IResponse rawResponse = await messageSender.Call(actorId, request, needException);
                    if (rawResponse is not T response)
                    {
                        (rawResponse as MessageObject)?.Dispose();
                        throw new Exception(
                            $"service discovery proxy service response type mismatch request: {requestName}, actual: {rawResponse?.GetType().Name}");
                    }

                    if (ShouldRetryServiceCallError(response.Error))
                    {
                        int error = response.Error;
                        string message = response.Message;
                        (response as MessageObject)?.Dispose();
                        throw new RpcException(error,
                            $"service discovery proxy retryable service call error request: {requestName} sceneType: {sceneTypeName} error: {error} message: {message}");
                    }

                    if (throwResponseError && response.Error != ErrorCode.ERR_Success)
                    {
                        int error = response.Error;
                        string message = response.Message;
                        (response as MessageObject)?.Dispose();
                        throw new RpcException(error,
                            $"service discovery proxy service call error request: {requestName} sceneType: {sceneTypeName} error: {error} message: {message}");
                    }

                    return response;
                }
                catch (RpcException rpcException)
                {
                    ++retryCount;
                    self = selfRef;
                    if (self == null)
                    {
                        throw new Exception("service discovery proxy disposed");
                    }

                    if (!ShouldRetryServiceCallError(rpcException.Error) || retryCount >= self.ServiceResolveRetryTimes)
                    {
                        throw;
                    }

                    await self.WaitResolveRetryDelayAsync();
                }
            }
        }

        /// <summary>
        /// 单次从本地缓存中按 <paramref name="sceneType"/> 查找服务列表，并通过确定性排序取模选择对应的 <see cref="ActorId"/>。
        /// </summary>
        private static ActorId ResolveServiceActorIdByRouteKeyOnce(this ServiceDiscoveryProxy self, int sceneType, string sceneTypeName,
            long routeKey)
        {
            List<ServiceInfo> localServices = self.GetBySceneType(sceneType);
            if (localServices.Count == 0)
            {
                throw new RpcException(ErrorCode.ERR_ServiceNotFound,
                    $"route service not found in local cache sceneType: {sceneTypeName}");
            }

            return SelectServiceActorIdByRouteKey(localServices, routeKey, sceneTypeName);
        }

        /// <summary>
        /// 判断服务解析异常是否可重试。
        /// </summary>
        private static bool ShouldRetryResolveService(this ServiceDiscoveryProxy self, RpcException rpcException)
        {
            return rpcException.Error == ErrorCode.ERR_ServiceNotFound
                   || ServiceDiscoveryErrorHelper.ShouldTriggerFailover(rpcException.Error);
        }

        /// <summary>
        /// 判断服务调用返回的错误码是否属于可重试错误（如服务未找到、目标 Actor 不存在或超时）。
        /// </summary>
        private static bool ShouldRetryServiceCallError(int error)
        {
            return error == ErrorCode.ERR_ServiceNotFound
                   || ServiceDiscoveryErrorHelper.ShouldTriggerFailover(error);
        }

        /// <summary>
        /// 对候选服务列表按确定性规则（SceneName -> IP -> Port -> Fiber -> InstanceId）排序后，根据 <paramref name="routeKey"/> 取模选出目标 <see cref="ActorId"/>。
        /// </summary>
        private static ActorId SelectServiceActorIdByRouteKey(List<ServiceInfo> serviceInfos, long routeKey, string sceneTypeName)
        {
            if (serviceInfos == null || serviceInfos.Count == 0)
            {
                throw new RpcException(ErrorCode.ERR_ServiceNotFound,
                    $"route service not found sceneType: {sceneTypeName}");
            }

            List<ServiceInfo> orderedServices = new(serviceInfos.Count);
            foreach (ServiceInfo serviceInfo in serviceInfos)
            {
                ValidateServiceInfo(serviceInfo, sceneTypeName);
                orderedServices.Add(serviceInfo);
            }

            orderedServices.Sort(CompareServiceInfo);
            return orderedServices[GetRouteIndex(routeKey, orderedServices.Count)].ActorId;
        }

        /// <summary>
        /// 将有符号 <paramref name="routeKey"/> 转为无符号整数并对服务数量取模，得到路由下标。
        /// </summary>
        private static int GetRouteIndex(long routeKey, int count)
        {
            if (count <= 0)
            {
                throw new Exception("route service count must be greater than zero");
            }

            ulong normalizedRouteKey = unchecked((ulong)routeKey);
            return (int)(normalizedRouteKey % (ulong)count);
        }

        /// <summary>
        /// 确定性比较两个 <see cref="ServiceInfo"/>（先按 SceneName 字典序，再按 ActorId 排序），保证各节点路由结果一致。
        /// </summary>
        private static int CompareServiceInfo(ServiceInfo left, ServiceInfo right)
        {
            int compare = string.Compare(left?.SceneName, right?.SceneName, StringComparison.Ordinal);
            if (compare != 0)
            {
                return compare;
            }

            return CompareActorId(left?.ActorId ?? default, right?.ActorId ?? default);
        }

        /// <summary>
        /// 确定性比较两个 <see cref="ActorId"/>。
        /// </summary>
        private static int CompareActorId(ActorId left, ActorId right)
        {
            int compare = string.Compare(left.Address.IP, right.Address.IP, StringComparison.Ordinal);
            if (compare != 0)
            {
                return compare;
            }

            compare = left.Address.Port.CompareTo(right.Address.Port);
            if (compare != 0)
            {
                return compare;
            }

            compare = left.FiberInstanceId.Fiber.CompareTo(right.FiberInstanceId.Fiber);
            if (compare != 0)
            {
                return compare;
            }

            return left.FiberInstanceId.InstanceId.CompareTo(right.FiberInstanceId.InstanceId);
        }

        /// <summary>
        /// 校验缓存中的 <see cref="ServiceInfo"/> 是否合法。
        /// </summary>
        private static void ValidateServiceInfo(ServiceInfo serviceInfo, string sceneTypeName)
        {
            if (serviceInfo == null || string.IsNullOrWhiteSpace(serviceInfo.SceneName) || serviceInfo.ActorId == default)
            {
                throw new RpcException(ErrorCode.ERR_ServiceDiscoveryOperationFailed,
                    $"route service invalid local cache sceneType: {sceneTypeName}");
            }
        }

        /// <summary>
        /// 等待服务解析重试间隔。
        /// </summary>
        private static async ETTask WaitResolveRetryDelayAsync(this ServiceDiscoveryProxy self)
        {
            int delay = self.ServiceResolveRetryIntervalMs;
            if (delay <= 0)
            {
                delay = 1;
            }

            Scene root = self.Root();
            EntityRef<Scene> rootRef = root;
            await root.TimerComponent.WaitAsync(delay);
            root = rootRef;
            if (root == null)
            {
                throw new Exception("service discovery proxy root disposed during retry delay");
            }
        }

        /// <summary>
        /// 清空 Proxy 本地的全部服务缓存与索引。
        /// </summary>
        private static void ClearLocalServices(this ServiceDiscoveryProxy self, bool publishRemoveEvent)
        {
            using ListComponent<string> sceneNames = ListComponent<string>.Create();
            foreach ((string sceneName, EntityRef<ServiceInfo> _) in self.SceneNameServices)
            {
                sceneNames.Add(sceneName);
            }

            foreach (string sceneName in sceneNames)
            {
                self.RemoveLocalService(sceneName, publishRemoveEvent);
            }
        }

        /// <summary>
        /// 新增或更新 Proxy 本地的服务缓存，并向当前业务场景发布 <see cref="OnServiceChangeAddService"/> 事件。
        /// </summary>
        private static void AddOrUpdateLocalService(this ServiceDiscoveryProxy self, ServiceInfoProto serviceInfoProto)
        {
            if (serviceInfoProto == null || string.IsNullOrEmpty(serviceInfoProto.SceneName) || serviceInfoProto.ActorId == default)
            {
                return;
            }

            if (self.SceneNameServices.TryGetValue(serviceInfoProto.SceneName, out EntityRef<ServiceInfo> serviceRef))
            {
                ServiceInfo existingServiceInfo = serviceRef;
                if (ServiceDiscoveryHelper.IsSameService(existingServiceInfo, serviceInfoProto.ActorId, serviceInfoProto.Metadata))
                {
                    return;
                }
            }

            self.RemoveLocalService(serviceInfoProto.SceneName, false);

            ServiceInfo serviceInfo = self.AddChild<ServiceInfo, string, ActorId>(serviceInfoProto.SceneName, serviceInfoProto.ActorId);
            serviceInfo.Metadata = ServiceDiscoveryHelper.CloneMetadata(serviceInfoProto.Metadata);
            self.SceneNameServices[serviceInfo.SceneName] = serviceInfo;
            ServiceDiscoveryHelper.AddToIndexes(self.ServicesIndexs, self.Indexs, serviceInfo.SceneName, serviceInfo.Metadata);

            EventSystem.Instance.Publish(self.Scene(), new OnServiceChangeAddService { ServiceName = serviceInfoProto.SceneName });
        }

        /// <summary>
        /// 从 Proxy 本地缓存中移除指定服务，并按需向当前业务场景发布 <see cref="OnServiceChangeRemoveService"/> 事件。
        /// </summary>
        private static void RemoveLocalService(this ServiceDiscoveryProxy self, string sceneName, bool publishRemoveEvent)
        {
            bool removed = ServiceDiscoveryHelper.RemoveLocalServiceCache(self.SceneNameServices, self.ServicesIndexs, self.Indexs, sceneName);
            if (!removed)
            {
                return;
            }

            if (publishRemoveEvent)
            {
                EventSystem.Instance.Publish(self.Scene(), new OnServiceChangeRemoveService { ServiceName = sceneName });
            }
        }

        /// <summary>
        /// 处理单条服务变更通知。
        /// </summary>
        private static void OnServiceChangeNotification(this ServiceDiscoveryProxy self, int changeType,
            ServiceInfoProto serviceInfoProto)
        {
            switch (changeType)
            {
                case ServiceChangeTypeAdd:
                    self.AddOrUpdateLocalService(serviceInfoProto);
                    break;
                case ServiceChangeTypeRemove:
                    if (serviceInfoProto == null || string.IsNullOrEmpty(serviceInfoProto.SceneName))
                    {
                        break;
                    }

                    self.RemoveLocalService(serviceInfoProto.SceneName, true);
                    break;
            }
        }

        /// <summary>
        /// 处理服务变更通知
        /// </summary>
        public static void OnServiceChangeNotification(this ServiceDiscoveryProxy self, int changeType,
            List<ServiceInfoProto> serviceInfos)
        {
            self.OnServiceChangeNotification(changeType, serviceInfos, ServiceChangeSourceAgentPush);
        }

        /// <summary>
        /// 批量处理带来源标识的服务变更通知列表并记录日志。
        /// </summary>
        private static void OnServiceChangeNotification(this ServiceDiscoveryProxy self, int changeType,
            List<ServiceInfoProto> serviceInfos, string source)
        {
            if (serviceInfos == null || serviceInfos.Count == 0)
            {
                Log.Info(
                    $"ServiceDiscoveryProxy update proxy: {self.RootName} source: {source} changeType: {GetServiceChangeTypeName(changeType)} serviceCount: 0");
                return;
            }

            foreach (ServiceInfoProto serviceInfo in serviceInfos)
            {
                self.LogServiceChange(changeType, serviceInfo, source);
                self.OnServiceChangeNotification(changeType, serviceInfo);
            }
        }

        /// <summary>
        /// 打印服务变更详情日志。
        /// </summary>
        private static void LogServiceChange(this ServiceDiscoveryProxy self, int changeType, ServiceInfoProto serviceInfoProto,
            string source)
        {
            string proxySceneName = string.IsNullOrWhiteSpace(self.RootName) ? self.Root()?.Name : self.RootName;
            if (string.IsNullOrWhiteSpace(proxySceneName))
            {
                proxySceneName = "<unknown>";
            }

            string resolvedSource = string.IsNullOrWhiteSpace(source) ? ServiceChangeSourceAgentPush : source;
            Log.Info(
                $"ServiceDiscoveryProxy update proxy: {proxySceneName} source: {resolvedSource} changeType: {GetServiceChangeTypeName(changeType)} service: {FormatServiceInfo(serviceInfoProto)}");
        }

        /// <summary>
        /// 将变更类型枚举值转换为可读字符串（add / remove）。
        /// </summary>
        private static string GetServiceChangeTypeName(int changeType)
        {
            return changeType switch
            {
                ServiceChangeTypeAdd => "add",
                ServiceChangeTypeRemove => "remove",
                _ => $"unknown({changeType})"
            };
        }

        /// <summary>
        /// 格式化 <see cref="ServiceInfoProto"/> 为日志文本。
        /// </summary>
        private static string FormatServiceInfo(ServiceInfoProto serviceInfoProto)
        {
            if (serviceInfoProto == null)
            {
                return "{null}";
            }

            string sceneName = string.IsNullOrWhiteSpace(serviceInfoProto.SceneName) ? "<empty>" : serviceInfoProto.SceneName;
            return
                $"{{sceneName={sceneName}, actorId={serviceInfoProto.ActorId}, metadata={FormatMetadata(serviceInfoProto.Metadata)}}}";
        }

        /// <summary>
        /// 格式化元数据字典为有序键值对字符串。
        /// </summary>
        private static string FormatMetadata(StringKV metadata)
        {
            if (metadata == null || metadata.Count == 0)
            {
                return "{}";
            }

            List<string> items = new(metadata.Count);
            foreach ((string key, string value) in metadata)
            {
                string normalizedKey = string.IsNullOrWhiteSpace(key) ? "<empty>" : key;
                string normalizedValue = value ?? "<null>";
                items.Add($"{normalizedKey}={normalizedValue}");
            }

            items.Sort(StringComparer.Ordinal);
            return $"{{{string.Join(", ", items)}}}";
        }

        #region 获取ServiceInfo

        /// <summary>
        /// 根据场景名称获取本地缓存的 <see cref="ServiceInfo"/>（若不存在则抛出异常）。
        /// </summary>
        public static ServiceInfo GetServiceInfo(this ServiceDiscoveryProxy self, string sceneName)
        {
            if (!self.SceneNameServices.TryGetValue(sceneName, out EntityRef<ServiceInfo> serviceInfoRef))
            {
                throw new Exception("not found scene name: " + sceneName);
            }

            return serviceInfoRef;
        }

        /// <summary>
        /// 根据 <see cref="SceneType"/> 获取本地缓存的所有匹配服务列表。
        /// </summary>
        public static List<ServiceInfo> GetBySceneType(this ServiceDiscoveryProxy self, int sceneType)
        {
            return self.GetServiceInfoByFilter(new StringKV
            {
                { ServiceMetaKey.SceneType, SceneTypeSingleton.Instance.GetSceneName(sceneType) }
            });
        }

        /// <summary>
        /// 根据 <see cref="SceneType"/> 与区服号（Zone）获取本地缓存的所有匹配服务列表。
        /// </summary>
        public static List<ServiceInfo> GetBySceneTypeAndZone(this ServiceDiscoveryProxy self, int sceneType, int zone)
        {
            return self.GetServiceInfoByFilter(new StringKV
            {
                { ServiceMetaKey.SceneType, SceneTypeSingleton.Instance.GetSceneName(sceneType) },
                { ServiceMetaKey.Zone, zone.ToString() }
            });
        }

        /// <summary>
        /// 查询服务
        /// </summary>
        public static List<ServiceInfo> GetServiceInfoByFilter(this ServiceDiscoveryProxy self, StringKV filterMetadata)
        {
            return ServiceDiscoveryHelper.GetServiceInfoByFilter(self.SceneNameServices, self.ServicesIndexs, filterMetadata);
        }

        /// <summary>
        /// 按场景名称直接索引获取 <see cref="ServiceInfo"/>。
        /// </summary>
        public static ServiceInfo GetByName(this ServiceDiscoveryProxy self, string name)
        {
            return self.SceneNameServices[name];
        }

        #endregion

        #region 兼容调用面

        /// <summary>
        /// 根据目标服务场景名称查找 ActorId 并发送 RPC 请求。
        /// </summary>
        public static async ETTask<IResponse> Call(this ServiceDiscoveryProxy self, string sceneName, IRequest request, bool needException = true)
        {
            ServiceInfo serviceInfo = self.GetServiceInfo(sceneName);
            MessageSender messageSender = self.Root().GetComponent<MessageSender>();
            if (messageSender == null)
            {
                throw new Exception($"service discovery proxy message sender is null, scene: {self.Root().Name}");
            }

            return await messageSender.Call(serviceInfo.ActorId, request, needException);
        }

        #endregion
    }
}

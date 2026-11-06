using System;

namespace ET.Server
{
    /// <summary>
    /// 单一位置类型（如 <c>LocationType.Unit</c> 或 <c>LocationType.GateSession</c>）的 ActorLocation 消息发送器管理组件系统（<see cref="MessageLocationSenderOneType"/>）。
    /// 负责按目标实体 ID 维护对应的 <see cref="MessageLocationSender"/> 缓存，首次发送时通过 <see cref="LocationProxyComponent"/> 查询 <see cref="ActorId"/>，并在收到 <see cref="ErrorCode.ERR_NotFoundActor"/>（目标实体发生跨服迁移）时清空旧 ActorId 缓存、等待并重新查询新位置重发。
    /// </summary>
    [EntitySystemOf(typeof(MessageLocationSenderOneType))]
    public static partial class MessageLocationSenderComponentSystem
    {
        /// <summary>
        /// 定期扫描并回收长时间无收发活动的 <see cref="MessageLocationSender"/> 缓存项的定时器。
        /// </summary>
        [Invoke(TimerInvokeType.MessageLocationSenderChecker)]
        public class MessageLocationSenderChecker: ATimer<MessageLocationSenderOneType>
        {
            protected override void Run(MessageLocationSenderOneType self)
            {
                try
                {
                    self.Check();
                }
                catch (Exception e)
                {
                    Log.Error($"move timer error: {self.Id}\n{e}");
                }
            }
        }
    
        [EntitySystem]
        private static void Awake(this MessageLocationSenderOneType self)
        {
            // 每10s扫描一次过期的actorproxy进行回收,过期时间是2分钟
            // 可能由于bug或者进程挂掉，导致ActorLocationSender发送的消息没有确认，结果无法自动删除，每一分钟清理一次这种ActorLocationSender
            self.CheckTimer = self.Root().TimerComponent.NewRepeatedTimer(10 * 1000, TimerInvokeType.MessageLocationSenderChecker, self);
        }
        
        [EntitySystem]
        private static void Destroy(this MessageLocationSenderOneType self)
        {
            Scene root = self.Root();
            if (root != null)
            {
                root.TimerComponent?.Remove(ref self.CheckTimer);    
            }
        }

        /// <summary>
        /// 扫描所有子 <see cref="MessageLocationSender"/>，移除超过 <see cref="MessageLocationSenderOneType.TIMEOUT_TIME"/> 未使用的缓存项。
        /// </summary>
        private static void Check(this MessageLocationSenderOneType self)
        {
            using ListComponent<long> list = ListComponent<long>.Create();
            
            long timeNow = self.GetSingleton<TimeInfo>().ServerNow();
            foreach ((long key, Entity value) in self.Children)
            {
                MessageLocationSender messageLocationMessageSender = (MessageLocationSender) value;

                if (timeNow > messageLocationMessageSender.LastSendOrRecvTime + MessageLocationSenderOneType.TIMEOUT_TIME)
                {
                    list.Add(key);
                }
            }

            foreach (long id in list)
            {
                self.Remove(id);
            }
        }

        /// <summary>
        /// 获取或创建指定目标实体 ID 的 <see cref="MessageLocationSender"/>。
        /// </summary>
        private static MessageLocationSender GetOrCreate(this MessageLocationSenderOneType self, long id)
        {
            if (id == 0)
            {
                throw new Exception($"actor id is 0");
            }

            if (self.Children.TryGetValue(id, out Entity actorLocationSender))
            {
                return (MessageLocationSender) actorLocationSender;
            }

            actorLocationSender = self.AddChildWithId<MessageLocationSender>(id);
            return (MessageLocationSender) actorLocationSender;
        }

        // 有需要主动删除actorMessageSender的需求，比如断线重连，玩家登录了不同的Gate，这时候需要通知map删掉之前的actorMessageSender
        // 然后重新创建新的，重新请求新的ActorId
        /// <summary>
        /// 主动移除并销毁指定实体 ID 的 <see cref="MessageLocationSender"/> 缓存。
        /// </summary>
        public static void Remove(this MessageLocationSenderOneType self, long id)
        {
            if (!self.Children.TryGetValue(id, out Entity actorMessageSender))
            {
                return;
            }

            actorMessageSender.Dispose();
        }
        
        // 发给不会改变位置的actorlocation用这个，这种actor消息不会阻塞发送队列，性能更高
        // 发送过去找不到actor不会重试,用此方法，你得保证actor提前注册好了location
        /// <summary>
        /// 向位置固定、不会发生跨服迁移的实体发送普通单向消息（不等待 ACK，性能更高，但找不到目标时不会自动重试）。
        /// </summary>
        public static void Send(this MessageLocationSenderOneType self, long entityId, IMessage message)
        {
            self.SendInner(entityId, message).Coroutine();
        }

        /// <summary>
        /// 异步向位置固定的实体发送普通单向消息。
        /// </summary>
        public static async ETTask SendAsync(this MessageLocationSenderOneType self, long entityId, IMessage message)
        {
            await self.SendInner(entityId, message);
        }
        
        private static async ETTask SendInner(this MessageLocationSenderOneType self, long entityId, IMessage message)
        {
            EntityRef<MessageLocationSenderOneType> selfRef = self;
            MessageLocationSender messageLocationSender = self.GetOrCreate(entityId);
            EntityRef<MessageLocationSender> messageLocationSenderRef = messageLocationSender;
            Scene root = self.Root();
            EntityRef<Scene> rootRef = root;
            
            using (await root.CoroutineLockComponent.Wait(CoroutineLockType.MessageLocationSender, entityId))
            {
                messageLocationSender = messageLocationSenderRef;
                if (messageLocationSender == null)
                {
                    throw new RpcException(ErrorCode.ERR_MessageTimeout, $"{message}");
                }
                
                if (messageLocationSender.ActorId == default)
                {
                    root = rootRef;
                    self = selfRef;
                    ActorId actorId = await root.GetComponent<LocationProxyComponent>().Get((int)self.Id, messageLocationSender.Id);
                    messageLocationSender = messageLocationSenderRef;
                    if (messageLocationSender == null)
                    {
                        throw new RpcException(ErrorCode.ERR_ActorLocationSenderTimeout2, $"{message}");
                    }

                    if (actorId == default)
                    {
                        self = selfRef;
                        self?.Remove(entityId);
                        throw new RpcException(ErrorCode.ERR_NotFoundActor, $"{message}");
                    }

                    messageLocationSender.ActorId = actorId;
                }
                
                messageLocationSender = messageLocationSenderRef;
                messageLocationSender.LastSendOrRecvTime = messageLocationSender.GetSingleton<TimeInfo>().ServerNow();
                root = rootRef;
                root.GetComponent<MessageSender>().Send(messageLocationSender.ActorId, message);
            }
        }

        // 发给不会改变位置的actorlocation用这个，这种actor消息不会阻塞发送队列，性能更高，发送过去找不到actor不会重试
        // 发送过去找不到actor不会重试,用此方法，你得保证actor提前注册好了location
        /// <summary>
        /// 向位置固定的实体发起普通 RPC 调用（仅在解析 ActorId 阶段加锁，发送期间不阻塞后续消息队列）。
        /// </summary>
        public static async ETTask<IResponse> Call(this MessageLocationSenderOneType self, long entityId, IRequest request)
        {
            MessageLocationSender messageLocationSender = self.GetOrCreate(entityId);
            EntityRef<MessageLocationSender> messageLocationSenderRef = messageLocationSender;

            Scene root = self.Root();
            EntityRef<MessageLocationSenderOneType> selfRef = self;
            EntityRef<Scene> rootRef = root;
            
            using (await root.CoroutineLockComponent.Wait(CoroutineLockType.MessageLocationSender, entityId))
            {
                messageLocationSender = messageLocationSenderRef;
                if (messageLocationSender == null)
                {
                    throw new RpcException(ErrorCode.ERR_MessageTimeout, $"{request}");
                }

                if (messageLocationSender.ActorId == default)
                {
                    self = selfRef;
                    root = rootRef;
                    ActorId actorId = await root.GetComponent<LocationProxyComponent>().Get((int)self.Id, messageLocationSender.Id);
                    messageLocationSender = messageLocationSenderRef;
                    if (messageLocationSender == null)
                    {
                        throw new RpcException(ErrorCode.ERR_ActorLocationSenderTimeout2, $"{request}");
                    }
                    messageLocationSender.ActorId = actorId;
                }
            }

            messageLocationSender = messageLocationSenderRef;
            messageLocationSender.LastSendOrRecvTime = messageLocationSender.GetSingleton<TimeInfo>().ServerNow();
            root = rootRef;
            return await root.GetComponent<MessageSender>().Call(messageLocationSender.ActorId, request);
        }

        /// <summary>
        /// 向可跨服迁移的实体发送 <see cref="ILocationMessage"/> 消息（底层通过 <see cref="Call(MessageLocationSenderOneType,long,ILocationRequest)"/> 实现有序排队与位置迁移自动重发）。
        /// </summary>
        public static void Send(this MessageLocationSenderOneType self, long entityId, ILocationMessage message)
        {
            self.Call(entityId, message).Coroutine();
        }

        /// <summary>
        /// 向可跨服迁移的实体发起 <see cref="ILocationRequest"/> RPC 调用（全程持有 <see cref="CoroutineLockType.MessageLocationSender"/> 协程锁保证按序送达，遇到 <see cref="ErrorCode.ERR_NotFoundActor"/> 时自动重查新位置重试）。
        /// </summary>
        public static async ETTask<IResponse> Call(this MessageLocationSenderOneType self, long entityId, ILocationRequest iRequest)
        {
            EntityRef<MessageLocationSenderOneType> selfRef = self;
            MessageLocationSender messageLocationSender = self.GetOrCreate(entityId);
            long messageLocationSenderId = messageLocationSender.Id;
            EntityRef<MessageLocationSender> messageLocationSenderRef = messageLocationSender;
            Scene root = self.Root();
            Type iRequestType = iRequest.GetType();
            using (await root.CoroutineLockComponent.Wait(CoroutineLockType.MessageLocationSender, entityId))
            {
                messageLocationSender = messageLocationSenderRef;
                if (messageLocationSender == null)
                {
                    throw new RpcException(ErrorCode.ERR_NotFoundActor, $"{iRequest}");
                }

                try
                {
                    IResponse response = await self.CallInner(messageLocationSender, iRequest);
                    return response;
                }
                catch (RpcException)
                {
                    self = selfRef;
                    self.Remove(messageLocationSenderId);
                    throw;
                }
                catch (Exception e)
                {
                    self = selfRef;
                    self.Remove(messageLocationSenderId);
                    throw new Exception($"{iRequestType.FullName}", e);
                }
            }
        }

        /// <summary>
        /// 执行带位置重查与指数退避重试的 ActorLocation RPC 调用核心循环。
        /// </summary>
        private static async ETTask<IResponse> CallInner(this MessageLocationSenderOneType self, MessageLocationSender messageLocationSender, IRequest iRequest)
        {
            int failTimes = 0;
            EntityRef<MessageLocationSender> messageLocationSenderRef = messageLocationSender;
            EntityRef<MessageLocationSenderOneType> selfRef = self;
            messageLocationSender.LastSendOrRecvTime = self.GetSingleton<TimeInfo>().ServerNow();
            
            Scene root = self.Root();
            EntityRef<Scene> rootRef = root;
            
            Type requestType = iRequest.GetType();
            while (true)
            {
                self = selfRef;
                root = rootRef;
                messageLocationSender = messageLocationSenderRef;
                if (self == null || messageLocationSender == null)
                {
                    throw new RpcException(ErrorCode.ERR_ActorLocationSenderTimeout2, $"{iRequest}");
                }

                if (messageLocationSender.ActorId == default)
                {
                    long messageLocationSenderEntityId = messageLocationSender.Id;
                    ActorId locationActorId = await root.GetComponent<LocationProxyComponent>().Get((int)self.Id, messageLocationSenderEntityId);
                    messageLocationSender = messageLocationSenderRef;
                    if (messageLocationSender == null)
                    {
                        throw new RpcException(ErrorCode.ERR_ActorLocationSenderTimeout2, $"{iRequest}");
                    }

                    messageLocationSender.ActorId = locationActorId;
                }

                if (messageLocationSender.ActorId == default)
                {
                    return MessageHelper.CreateResponse(requestType, 0, ErrorCode.ERR_NotFoundActor);
                }

                root = rootRef;
                ActorId targetActorId = messageLocationSender.ActorId;
                IResponse response = await root.GetComponent<MessageSender>().Call(targetActorId, iRequest, needException: false);
                messageLocationSender = messageLocationSenderRef;
                if (messageLocationSender == null)
                {
                    throw new RpcException(ErrorCode.ERR_ActorLocationSenderTimeout3, $"{requestType.FullName}");
                }
                
                switch (response.Error)
                {
                    case ErrorCode.ERR_NotFoundActor:
                    {
                        // 如果没找到Actor,重试
                        ++failTimes;
                        if (failTimes > 10)
                        {
                            Log.Debug($"actor send message fail, actorid: {messageLocationSender.Id} {requestType.FullName}");
                            
                            // 这里删除actor，后面等待发送的消息会判断InstanceId，InstanceId不一致返回ERR_NotFoundActor
                            self = selfRef;
                            if (self != null)
                            {
                                self.Remove(messageLocationSender.Id);
                            }
                            return response;
                        }

                        // 等待0.5s再发送
                        root = rootRef;
                        await root.TimerComponent.WaitAsync(failTimes * failTimes * 10);
                        messageLocationSender = messageLocationSenderRef;
                        if (messageLocationSender == null)
                        {
                            throw new RpcException(ErrorCode.ERR_ActorLocationSenderTimeout4, $"{requestType.FullName}");
                        }

                        messageLocationSender.ActorId = default;
                        continue;
                    }
                    case ErrorCode.ERR_MessageTimeout:
                    {
                        throw new RpcException(response.Error, $"{requestType.FullName}");
                    }
                }

                if (ErrorCode.IsRpcNeedThrowException(response.Error))
                {
                    throw new RpcException(response.Error, $"Message: {response.Message} Request: {requestType.FullName}");
                }

                return response;
            }
        }
    }

    /// <summary>
    /// 场景级 ActorLocation 消息发送器总管理组件系统（<see cref="MessageLocationSenderComponent"/>）。
    /// 按 <c>locationType</c> 分桶管理多个 <see cref="MessageLocationSenderOneType"/> 子实体。
    /// </summary>
    [EntitySystemOf(typeof(MessageLocationSenderComponent))]
    public static partial class MessageLocationSenderManagerComponentSystem
    {
        [EntitySystem]
        private static void Awake(this MessageLocationSenderComponent self)
        {
        }
        
        /// <summary>
        /// 获取或创建指定 <paramref name="locationType"/> 对应的 <see cref="MessageLocationSenderOneType"/> 发送器容器。
        /// </summary>
        public static MessageLocationSenderOneType Get(this MessageLocationSenderComponent self, int locationType)
        {
            MessageLocationSenderOneType messageLocationSenderOneType = self.GetChild<MessageLocationSenderOneType>(locationType);
            if (messageLocationSenderOneType != null)
            {
                return messageLocationSenderOneType;
            }

            messageLocationSenderOneType = self.AddChildWithId<MessageLocationSenderOneType>(locationType);
            return messageLocationSenderOneType;
        }
    }
}

using System;

namespace ET
{
    /// <summary>
    /// 进程内 Actor 消息发送器系统（<see cref="ProcessInnerSender"/> 的逻辑扩展）。
    /// 负责同一个进程内部不同纤程（<see cref="Fiber"/>）之间的 Actor 消息投递、每帧从 <see cref="MessageQueue"/> 拉取消息分发到邮箱（<see cref="MailBoxComponent"/>），
    /// 以及管理 Actor RPC 调用的回调唤醒与超时检测。
    /// </summary>
    [EntitySystemOf(typeof(ProcessInnerSender))]
    public static partial class ProcessInnerSenderSystem
    {
        /// <summary>
        /// 组件销毁时，从全局进程内消息队列 <see cref="MessageQueue"/> 中注销当前纤程的消息队列。
        /// </summary>
        [EntitySystem]
        private static void Destroy(this ProcessInnerSender self)
        {
            Fiber fiber = self.Fiber();
            MessageQueue.Instance.RemoveQueue(fiber.Id);
        }

        /// <summary>
        /// 组件唤醒时，在全局进程内消息队列 <see cref="MessageQueue"/> 中为当前纤程注册专属的接收队列。
        /// </summary>
        [EntitySystem]
        private static void Awake(this ProcessInnerSender self)
        {
            Fiber fiber = self.Fiber();
            MessageQueue.Instance.AddQueue(fiber.Id);
        }

        /// <summary>
        /// 每帧轮询：从全局 <see cref="MessageQueue"/> 批量拉取发往当前纤程的消息（单帧最多 1000 条），并逐条分发处理。
        /// </summary>
        [EntitySystem]
        private static void Update(this ProcessInnerSender self)
        {
            self.list.Clear();
            Fiber fiber = self.Fiber();
            MessageQueue.Instance.Fetch(fiber, 1000, self.list);

            foreach (MessageInfo actorMessageInfo in self.list)
            {
                self.HandleMessage(fiber, actorMessageInfo);
            }
        }

        /// <summary>
        /// 处理单条从队列取出的进程内消息：
        /// 1. 若是 <see cref="IResponse"/> 响应消息，则直接唤醒本地等待的 RPC 回调；
        /// 2. 若是普通消息或请求消息，则根据目标 <c>InstanceId</c> 查找当前纤程内的 <see cref="MailBoxComponent"/> 并投递进去；若找不到目标邮箱且消息是 RPC 请求，则自动回复 <see cref="ErrorCode.ERR_NotFoundActor"/>。
        /// </summary>
        private static void HandleMessage(this ProcessInnerSender self, Fiber fiber, in MessageInfo messageInfo)
        {
            if (messageInfo.MessageObject is IResponse response)
            {
                self.HandleIActorResponse(response);
                return;
            }

            FiberInstanceId fiberInstanceId = messageInfo.FiberInstanceId;
            MessageObject message = messageInfo.MessageObject;
            long fromFiber = fiberInstanceId.Fiber;
            
            Entity entity = self.Fiber().Mailboxes.Get(fiberInstanceId.InstanceId);
            MailBoxComponent mailBoxComponent = entity as MailBoxComponent;
            if (mailBoxComponent == null)
            {
                Log.Warning($"actor not found mailbox, from: {fiberInstanceId} current: {fiber.Id} {message}");
                if (message is IRequest request)
                {
                    IResponse resp = MessageHelper.CreateResponse(request.GetType(), request.RpcId, ErrorCode.ERR_NotFoundActor);
                    self.Reply(fromFiber, resp);
                }
                return;
            }
            mailBoxComponent.Add(fromFiber, message);
        }

        /// <summary>
        /// 处理收到的 Actor RPC 响应消息，根据 <see cref="IResponse.RpcId"/> 取出挂起的回调结构并执行唤醒。
        /// </summary>
        private static void HandleIActorResponse(this ProcessInnerSender self, IResponse response)
        {
            if (!self.requestCallback.Remove(response.RpcId, out ProcessInnerMessageSenderStruct actorMessageSender))
            {
                return;
            }
            Run(actorMessageSender, response);
        }
        
        /// <summary>
        /// 根据响应错误码与调用方异常配置（<see cref="ProcessInnerMessageSenderStruct.NeedException"/>），决定是抛出 <see cref="RpcException"/> 还是正常返回响应结果。
        /// </summary>
        private static void Run(ProcessInnerMessageSenderStruct self, IResponse response)
        {
            if (response.Error == ErrorCode.ERR_MessageTimeout)
            {
                self.SetException(new RpcException(response.Error, $"Rpc error: request, 注意Actor消息超时，请注意查看是否死锁或者没有reply: actorId: {self.FiberInstanceId} {self.RequestType.FullName}, response: {response}"));
                return;
            }

            if (self.NeedException && ErrorCode.IsRpcNeedThrowException(response.Error))
            {
                self.SetException(new RpcException(response.Error, $"Rpc error: actorId: {self.FiberInstanceId} request: {self.RequestType.FullName}, response: {response}"));
                return;
            }

            self.SetResult(response);
        }
        
        /// <summary>
        /// 将 Actor RPC 的响应消息发回给请求来源纤程（<paramref name="fromFiber"/>）。
        /// </summary>
        /// <param name="self">当前进程内发送器。</param>
        /// <param name="fromFiber">发起请求的来源纤程 ID。</param>
        /// <param name="message">要发回的响应消息。</param>
        public static void Reply(this ProcessInnerSender self, long fromFiber, IResponse message)
        {
            self.SendInner(new FiberInstanceId(fromFiber, 0), (MessageObject)message);
        }

        /// <summary>
        /// 向目标 <see cref="FiberInstanceId"/> 发送一条单向 Actor 消息。
        /// </summary>
        /// <param name="self">当前进程内发送器。</param>
        /// <param name="fiberInstanceId">目标 Actor 的纤程与实体实例 ID。</param>
        /// <param name="message">要发送的消息对象。</param>
        public static void Send(this ProcessInnerSender self, FiberInstanceId fiberInstanceId, IMessage message)
        {
            self.SendInner(fiberInstanceId, (MessageObject)message);
        }

        /// <summary>
        /// 内部投递实现：调用 <see cref="MessageQueue.Send"/> 将消息放入目标纤程的内存接收队列。
        /// </summary>
        private static bool SendInner(this ProcessInnerSender self, FiberInstanceId fiberInstanceId, MessageObject message)
        {
            Fiber fiber = self.Fiber();
            
            // 如果发向同一个进程，则扔到消息队列中
            return MessageQueue.Instance.Send(fiber, fiberInstanceId, message);
        }

        /// <summary>
        /// 递增并获取下一个唯一的 RPC 请求 ID。
        /// </summary>
        private static int GetRpcId(this ProcessInnerSender self)
        {
            return ++self.RpcId;
        }

        /// <summary>
        /// 向目标 Actor 发起异步 RPC 请求（使用默认超时时间 <see cref="ProcessInnerSender.TIMEOUT_TIME"/>，即 40 秒）。
        /// </summary>
        /// <param name="self">当前进程内发送器。</param>
        /// <param name="fiberInstanceId">目标 Actor 的纤程与实体实例 ID。</param>
        /// <param name="request">RPC 请求消息。</param>
        /// <param name="needException">当返回错误码需要抛异常时，是否抛出 <see cref="RpcException"/>。</param>
        /// <returns>目标 Actor 返回的响应消息。</returns>
        public static async ETTask<IResponse> Call(
                this ProcessInnerSender self,
                FiberInstanceId fiberInstanceId,
                IRequest request,
                bool needException = true
        )
        {
            return await self.Call(fiberInstanceId, request, ProcessInnerSender.TIMEOUT_TIME, needException);
        }

        /// <summary>
        /// 向目标 Actor 发起带自定义超时时间的异步 RPC 请求，并监控耗时（超过 200ms 会输出慢调用警告日志）。
        /// </summary>
        /// <param name="self">当前进程内发送器。</param>
        /// <param name="fiberInstanceId">目标 Actor 的纤程与实体实例 ID。</param>
        /// <param name="request">RPC 请求消息。</param>
        /// <param name="timeout">超时时间（毫秒）。</param>
        /// <param name="needException">超时或错误时是否抛出异常。</param>
        /// <returns>目标 Actor 返回的响应消息。</returns>
        public static async ETTask<IResponse> Call(
                this ProcessInnerSender self,
                FiberInstanceId fiberInstanceId,
                IRequest request,
                long timeout,
                bool needException = true
        )
        {
            int rpcId = self.GetRpcId();
            request.RpcId = rpcId;
            
            if (fiberInstanceId == default)
            {
                throw new Exception($"FiberInstanceId id is 0: {request}");
            }
            Fiber fiber = self.Fiber();
            Type requestType = request.GetType();
            
            IResponse response;
            if (!self.SendInner(fiberInstanceId, (MessageObject)request))  // 纤程不存在
            {
                response = MessageHelper.CreateResponse(requestType, rpcId, ErrorCode.ERR_NotFoundActor);
                return response;
            }
            
            ProcessInnerMessageSenderStruct messageSenderStruct = new(fiberInstanceId, requestType, needException);
            self.requestCallback.Add(rpcId, messageSenderStruct);
            
            // 启动异步超时计时协程：若在 timeout 毫秒内仍未收到响应，则自动移除回调并触发超时异常或返回超时错误码
            async ETTask Timeout()
            {
                EntityRef<ProcessInnerSender> selfRef = self;
                await fiber.Root.TimerComponent.WaitAsync(timeout);
                self = selfRef;
                if (!self.requestCallback.Remove(rpcId, out ProcessInnerMessageSenderStruct action))
                {
                    return;
                }
                
                if (needException)
                {
                    action.SetException(new Exception($"actor sender timeout: {requestType.FullName}"));
                }
                else
                {
                    IResponse response = MessageHelper.CreateResponse(requestType, rpcId, ErrorCode.ERR_MessageTimeout);
                    action.SetResult(response);
                }
            }
            
            Timeout().Coroutine();
            
            long beginTime = fiber.GetSingleton<TimeInfo>().ServerNow();

            response = await messageSenderStruct.Wait();
            
            long endTime = fiber.GetSingleton<TimeInfo>().ServerNow();

            // 监控 Actor RPC 耗时，超过 200ms 打印警告方便定位性能瓶颈或慢请求
            long costTime = endTime - beginTime;
            if (costTime > 200)
            {
                Log.Warning($"actor rpc time > 200: {costTime} {requestType.FullName}");
            }
            
            return response;
        }
    }
}

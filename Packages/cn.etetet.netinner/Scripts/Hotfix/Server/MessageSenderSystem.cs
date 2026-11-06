using System;
using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 业务 Fiber 统一 Actor 消息发送器（<see cref="MessageSender"/>）的系统类。
    /// <para>对上层业务屏蔽“同进程通信”与“跨进程通信”的差异：若目标 <see cref="ActorId.Address"/> 与本进程内网地址相同，则直接走内存无锁队列（<see cref="ProcessInnerSender"/>）；若在不同进程，则包装为 <see cref="A2NetInner_Message"/> 或 <see cref="A2NetInner_Request"/> 转发给本进程的 <c>NetInner</c> 纤程通过内网 Socket 发送。</para>
    /// </summary>
    [EntitySystemOf(typeof(MessageSender))]
    public static partial class MessageSenderSystem
    {
        [EntitySystem]
        private static void Awake(this MessageSender self)
        {
            self.ProcessInnerSender = self.Root().GetComponent<ProcessInnerSender>();
        }
        
        /// <summary>
        /// 向指定 <paramref name="actorId"/> 发送单向 Actor 消息（自动识别同进程内存投递或跨进程 <c>NetInner</c> 转发）。
        /// </summary>
        public static void Send(this MessageSender self, ActorId actorId, IMessage message)
        {
            Fiber fiber = self.Fiber();
            // 如果发向同一个进程，则扔到消息队列中
            if (actorId.Address == self.GetSingleton<AddressSingleton>().InnerAddress)
            {
                self.ProcessInnerSender.Send(actorId.FiberInstanceId, message);
                return;
            }

            // 发给NetInner纤程
            A2NetInner_Message a2NetInnerMessage = A2NetInner_Message.Create();
            a2NetInnerMessage.FromFiber = fiber.Id;
            a2NetInnerMessage.ActorId = actorId;
            a2NetInnerMessage.MessageObject = message;

            FiberInstanceId netInnerFiberInstanceId = self.GetSingleton<ProcessFiberAddressSingleton>().Get(SceneType.NetInner);
            MessageQueue.Instance.Send(fiber, netInnerFiberInstanceId, a2NetInnerMessage);
        }

        /// <summary>
        /// 向指定 <paramref name="actorId"/> 发送 Actor RPC 请求并等待响应（使用默认超时时间 <see cref="ProcessInnerSender.TIMEOUT_TIME"/>）。
        /// </summary>
        public static async ETTask<IResponse> Call(this MessageSender self, ActorId actorId, IRequest request, bool needException = true)
        {
            return await self.Call(actorId, request, ProcessInnerSender.TIMEOUT_TIME, needException);
        }

        /// <summary>
        /// 向指定 <paramref name="actorId"/> 发送带自定义超时时间的 Actor RPC 请求并等待响应。
        /// </summary>
        public static async ETTask<IResponse> Call(this MessageSender self, ActorId actorId, IRequest request, long timeout,
            bool needException = true)
        {
            if (actorId == default)
            {
                throw new Exception($"actor id is 0: {request}");
            }
            
            IResponse response;
            if (actorId.Address == self.GetSingleton<AddressSingleton>().InnerAddress)
            {
                response = await self.ProcessInnerSender.Call(actorId.FiberInstanceId, request, timeout, needException);
            }
            else
            {
                // 发给NetInner纤程
                A2NetInner_Request a2NetInner_Request = A2NetInner_Request.Create();
                a2NetInner_Request.ActorId = actorId;
                a2NetInner_Request.MessageObject = request;

                FiberInstanceId netInnerFiberInstanceId = self.GetSingleton<ProcessFiberAddressSingleton>().Get(SceneType.NetInner);
                using A2NetInner_Response a2NetInnerResponse = await self.ProcessInnerSender.Call(
                    netInnerFiberInstanceId, a2NetInner_Request, timeout) as A2NetInner_Response;
                response = a2NetInnerResponse.MessageObject;
            }

            if (response.Error == ErrorCode.ERR_MessageTimeout)
            {
                throw new RpcException(response.Error, $"Rpc error: request, 注意Actor消息超时，请注意查看是否死锁或者没有reply: actorId: {actorId} {request}, response: {response}");
            }
            if (needException && ErrorCode.IsRpcNeedThrowException(response.Error))
            {
                throw new RpcException(response.Error, $"Rpc error: actorId: {actorId} {request}, response: {response}");
            }
            return response;
        }
    }
}

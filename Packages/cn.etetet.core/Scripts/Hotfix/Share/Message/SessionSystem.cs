using System;
using System.Linq;

namespace ET
{
    /// <summary>
    /// 网络会话系统（<see cref="Session"/> 的逻辑扩展）。
    /// 提供会话生命周期管理、单向消息发送（<see cref="Send(ET.Session,ET.IMessage)"/>）、RPC 远程调用（<see cref="Call(ET.Session,ET.IRequest)"/>）及 RPC 响应回调唤醒等功能。
    /// </summary>
    [EntitySystemOf(typeof(Session))]
    public static partial class SessionSystem
    {
        /// <summary>
        /// 会话创建唤醒：绑定底层网络服务、初始化最后收发时间戳并清空 RPC 回调字典。
        /// </summary>
        [EntitySystem]
        private static void Awake(this Session self, AService aService)
        {
            self.AService = aService;
            long timeNow = self.GetSingleton<TimeInfo>().ClientNow();
            self.LastRecvTime = timeNow;
            self.LastSendTime = timeNow;

            self.requestCallbacks.Clear();
            
            Log.Info($"session create: zone: {self.Zone()} id: {self.Id} fiber: {self.Fiber().Id} {timeNow} ");
        }
        
        /// <summary>
        /// 会话销毁：从底层网络服务移除对应通道，并将所有尚未收到响应的挂起 RPC 调用以 <see cref="RpcException"/> 异常唤醒，避免调用方永远卡死。
        /// </summary>
        [EntitySystem]
        private static void Destroy(this Session self)
        {
            Scene root = self.Root();
            if (root == null)
            {
                return;
            }
            
            // 通知底层服务关闭并移除该网络连接通道
            self.AService.Remove(self.Id, self.Error);
            
            // 遍历所有等待中的 RPC 回调，抛出连接断开的异常以解除协程等待
            foreach (RpcInfo responseCallback in self.requestCallbacks.Values.ToArray())
            {
                responseCallback.SetException(new RpcException(self.Error, $"session dispose: {self.Id} {self.RemoteAddress}"));
            }

            Log.Info($"session dispose: {self.RemoteAddress} id: {self.Id} ErrorCode: {self.Error}, please see ErrorCode.cs! {self.GetSingleton<TimeInfo>().ClientNow()}");
            
            self.requestCallbacks.Clear();
        }
        
        /// <summary>
        /// 收到对端发回的 RPC 响应消息时调用：根据 <see cref="IResponse.RpcId"/> 查找并唤醒等待中的 <see cref="RpcInfo"/> 协程。
        /// </summary>
        /// <param name="self">当前网络会话。</param>
        /// <param name="response">收到的 RPC 响应消息。</param>
        public static void OnResponse(this Session self, IResponse response)
        {
            LogMsg.Instance.Recv(self.Fiber(), response);
            if (!self.requestCallbacks.Remove(response.RpcId, out RpcInfo action))
            {
                return;
            }
            action.SetResult(response);
        }

        /// <summary>
        /// 发起异步 RPC 远程过程调用，并挂起等待对端返回 <see cref="IResponse"/>。
        /// 支持通过当前协程上下文中的 <see cref="ETCancellationToken"/> 随时取消等待。
        /// </summary>
        /// <param name="self">当前网络会话。</param>
        /// <param name="request">要发送的 RPC 请求消息。</param>
        /// <returns>对端返回的 RPC 响应消息（若被取消则返回错误码为 <see cref="ErrorCode.ERR_Cancel"/> 的响应实例）。</returns>
        public static async ETTask<IResponse> Call(this Session self, IRequest request)
        {
            // 自增分配唯一的 RpcId，将请求与未来的响应关联起来
            int rpcId = ++self.RpcId;
            RpcInfo rpcInfo = new(request.GetType());
            self.requestCallbacks[rpcId] = rpcInfo;
            
            request.RpcId = rpcId;

            self.Send(request);
            
            // 若外部通过 ETCancellationToken 取消了本次调用，则移除回调并构造一个 Error = ERR_Cancel 的响应返回
            void CancelAction()
            {
                if (!self.requestCallbacks.Remove(rpcId, out RpcInfo action))
                {
                    return;
                }

                Type responseType = OpcodeType.Instance.GetResponseType(action.RequestType);
                IResponse response = (IResponse) Activator.CreateInstance(responseType);
                response.Error = ErrorCode.ERR_Cancel;
                action.SetResult(response);
            }

            ETCancellationToken cancellationToken = await ETTask.GetContextAsync<ETCancellationToken>();
            IResponse ret;
            try
            {
                cancellationToken?.Add(CancelAction);
                ret = await rpcInfo.Wait();
            }
            finally
            {
                cancellationToken?.Remove(CancelAction);
            }
            return ret;
        }

        /// <summary>
        /// 发起带超时限制的异步 RPC 远程过程调用。
        /// </summary>
        /// <param name="self">当前网络会话。</param>
        /// <param name="request">要发送的 RPC 请求消息。</param>
        /// <param name="timeout">超时时间（毫秒）。</param>
        /// <returns>对端返回的 RPC 响应消息。</returns>
        public static async ETTask<IResponse> Call(this Session self, IRequest request, int timeout)
        {
            return await self.Call(request).TimeoutAsync(timeout);
        }

        /// <summary>
        /// 向对端发送一条不需要响应的单向网络消息。
        /// </summary>
        /// <param name="self">当前网络会话。</param>
        /// <param name="message">要发送的消息对象。</param>
        public static void Send(this Session self, IMessage message)
        {
            self.Send(default, message);
        }
        
        /// <summary>
        /// 向对端发送网络消息（可附带目标 <see cref="FiberInstanceId"/>，常用于网关转发 Actor 消息）。
        /// 内部会更新最后发送时间、将消息序列化为 <see cref="MemoryBuffer"/> 并交由底层网络服务发送。
        /// </summary>
        /// <param name="self">当前网络会话。</param>
        /// <param name="fiberInstanceId">目标 Actor 的纤程实例 ID（普通外网消息传 default 即可）。</param>
        /// <param name="message">要发送的消息对象。</param>
        public static void Send(this Session self, FiberInstanceId fiberInstanceId, IMessage message)
        {
            LogMsg.Instance.Send(self.Fiber(), message);
            
            self.LastSendTime = self.GetSingleton<TimeInfo>().ClientNow();

            (ushort opcode, MemoryBuffer memoryBuffer) = MessageSerializeHelper.ToMemoryBuffer(self.AService, fiberInstanceId, message);
            
            self.AService.Send(self.Id, memoryBuffer);
        }
    }
}
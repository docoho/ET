using System;
using System.IO;

namespace ET
{
    // 知道对方的instanceId，使用这个类发actor消息
    /// <summary>
    /// 进程内 Actor RPC 请求的挂起等待上下文结构体。
    /// <remarks>
    /// 当通过 <see cref="ProcessInnerSender"/> 发起 <c>Call</c> 请求时，会创建一个此结构体并按 <c>RpcId</c> 存入字典，
    /// 内部持有一个池化的 <see cref="ETTask{IResponse}"/>，直到收到对应 <c>RpcId</c> 的响应消息或超时时唤醒调用协程。
    /// </remarks>
    /// </summary>
    public readonly struct ProcessInnerMessageSenderStruct
    {
        /// <summary>
        /// 目标 Actor 的 <see cref="ET.FiberInstanceId"/>。
        /// </summary>
        public FiberInstanceId FiberInstanceId { get; }
        
        /// <summary>
        /// 发送的请求消息类型（用于在超时或异常时构造配对的错误响应）。
        /// </summary>
        public Type RequestType { get; }
        
        private readonly ETTask<IResponse> tcs;

        /// <summary>
        /// 当 RPC 返回带有异常标志的错误码或超时时，是否需要抛出 <see cref="RpcException"/>。
        /// </summary>
        public bool NeedException { get; }
        
        public ProcessInnerMessageSenderStruct(FiberInstanceId fiberInstanceId, Type requestType, bool needException)
        {
            this.FiberInstanceId = fiberInstanceId;
            
            this.RequestType = requestType;
            
            this.tcs = ETTask<IResponse>.Create(true);
            this.NeedException = needException;
        }
        
        /// <summary>
        /// 收到 RPC 响应时设置结果并唤醒等待协程。
        /// </summary>
        public void SetResult(IResponse response)
        {
            this.tcs.SetResult(response);
        }
        
        /// <summary>
        /// 发生异常时唤醒等待协程并在 <c>await</c> 处抛出该异常。
        /// </summary>
        public void SetException(Exception exception)
        {
            this.tcs.SetException(exception);
        }

        /// <summary>
        /// 异步等待 RPC 响应返回。
        /// </summary>
        public async ETTask<IResponse> Wait()
        {
            return await this.tcs;
        }
    }
}
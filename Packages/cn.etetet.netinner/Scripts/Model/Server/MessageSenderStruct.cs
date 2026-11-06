using System;
using System.IO;

namespace ET.Server
{
    /// <summary>
    /// 跨进程内网 Actor RPC 请求的挂起等待上下文结构体。
    /// <para>保存目标 <see cref="ActorId"/>、请求类型及用于唤醒调用协程的 <see cref="ETTask{IResponse}"/>。</para>
    /// </summary>
    // 知道对方的instanceId，使用这个类发actor消息
    public readonly struct MessageSenderStruct
    {
        public ActorId ActorId { get; }
        
        public Type RequestType { get; }
        
        private readonly ETTask<IResponse> tcs;

        public bool NeedException { get; }
        
        public MessageSenderStruct(ActorId actorId, Type requestType, bool needException)
        {
            this.ActorId = actorId;
            
            this.RequestType = requestType;
            
            this.tcs = ETTask<IResponse>.Create(true);
            this.NeedException = needException;
        }
        
        public void SetResult(IResponse response)
        {
            this.tcs.SetResult(response);
        }
        
        public void SetException(Exception exception)
        {
            this.tcs.SetException(exception);
        }

        public async ETTask<IResponse> Wait()
        {
            return await this.tcs;
        }
    }
}
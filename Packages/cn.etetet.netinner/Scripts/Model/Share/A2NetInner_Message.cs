using MemoryPack;

namespace ET
{
    /// <summary>
    /// 进程内业务 Fiber 发往本进程 <c>NetInner</c> 纤程的跨进程单向消息包装类。
    /// </summary>
    [Message(1)]
    public class A2NetInner_Message: MessageObject, IMessage, IMessageWrapper
    {
        public static A2NetInner_Message Create()
        {
            return ObjectPool.Fetch(typeof(A2NetInner_Message)) as A2NetInner_Message;
        }

        public override void Dispose()
        {
            this.FromFiber = 0;
            this.ActorId = default;
            
            ObjectPool.Recycle(this);
        }
        
        /// <summary>发送方 Fiber ID。</summary>
        public long FromFiber;
        /// <summary>远端目标实体的 <see cref="ActorId"/>。</summary>
        public ActorId ActorId;
        /// <summary>实际要发送的内部消息对象。</summary>
        public IMessage MessageObject;
        
        public IMessage GetMessageObject()
        {
            return MessageObject;
        }
    }
    
    /// <summary>
    /// 进程内业务 Fiber 发往本进程 <c>NetInner</c> 纤程的跨进程 RPC 请求包装类。
    /// </summary>
    [Message(2)]
    [ResponseType(nameof(A2NetInner_Response))]
    public class A2NetInner_Request: MessageObject, IRequest, IMessageWrapper
    {
        public static A2NetInner_Request Create()
        {
            return ObjectPool.Fetch(typeof(A2NetInner_Request)) as A2NetInner_Request;
        }

        public override void Dispose()
        {
            this.RpcId = default;
            this.ActorId = default;
            this.MessageObject = default;
            
            ObjectPool.Recycle(this);
        }
        
        public int RpcId { get; set; }
        /// <summary>远端目标实体的 <see cref="ActorId"/>。</summary>
        public ActorId ActorId;
        /// <summary>实际要发送的内部 RPC 请求对象。</summary>
        public IRequest MessageObject;
        
        public IMessage GetMessageObject()
        {
            return this.MessageObject;
        }
    }
    
    /// <summary>
    /// <c>NetInner</c> 纤程收到远端进程 RPC 响应后回传给发起方业务 Fiber 的响应包装类。
    /// </summary>
    [Message(3)]
    public class A2NetInner_Response: MessageObject, IResponse, IMessageWrapper
    {
        public static A2NetInner_Response Create()
        {
            return ObjectPool.Fetch(typeof(A2NetInner_Response)) as A2NetInner_Response;
        }

        public override void Dispose()
        {
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            
            ObjectPool.Recycle(this);
        }
        
        public int Error { get; set; }
        public string Message { get; set; }
        public int RpcId { get; set; }
        
        public IResponse MessageObject;
        
        public IMessage GetMessageObject()
        {
            return this.MessageObject;
        }
    }
}

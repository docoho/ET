namespace ET
{
    /// <summary>
    /// 主纤程向 NetClient 网络纤程投递外网单向消息的进程内包装消息。
    /// </summary>
    [Message]
    public class A2NetClient_Message: MessageObject, IMessage, IMessageWrapper
    {
        public static A2NetClient_Message Create()
        {
            return ObjectPool.Fetch(typeof(A2NetClient_Message)) as A2NetClient_Message;
        }

        public override void Dispose()
        {
            this.MessageObject = default;
            ObjectPool.Recycle(this);
        }
        
        /// <summary>要通过外网 Session 发送的实际消息对象</summary>
        public IMessage MessageObject;
        
        public IMessage GetMessageObject()
        {
            return this.MessageObject;
        }
    }
    
    /// <summary>
    /// 主纤程向 NetClient 网络纤程发起外网 RPC 请求的进程内包装请求。
    /// </summary>
    [Message]
    [ResponseType(nameof(A2NetClient_Response))]
    public class A2NetClient_Request: MessageObject, IRequest, IMessageWrapper
    {
        public static A2NetClient_Request Create()
        {
            return ObjectPool.Fetch(typeof(A2NetClient_Request)) as A2NetClient_Request;
        }

        public override void Dispose()
        {
            this.RpcId = default;
            this.MessageObject = default;
            ObjectPool.Recycle(this);
        }
     
        public int RpcId { get; set; }
        /// <summary>要通过外网 Session.Call 发送的实际 RPC 请求对象</summary>
        public IRequest MessageObject;
        
        public IMessage GetMessageObject()
        {
            return this.MessageObject;
        }
    }
    
    /// <summary>
    /// NetClient 网络纤程向主纤程返回外网 RPC 响应的进程内包装响应。
    /// </summary>
    [Message]
    public class A2NetClient_Response: MessageObject, IResponse, IMessageWrapper
    {
        public static A2NetClient_Response Create()
        {
            return ObjectPool.Fetch(typeof(A2NetClient_Response)) as A2NetClient_Response;
        }

        public override void Dispose()
        {
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.MessageObject = default;
            ObjectPool.Recycle(this);
        }

        public int RpcId { get; set; }
        public int Error { get; set; }
        public string Message { get; set; }
        
        /// <summary>从外网 Session.Call 收到的实际 RPC 响应对象</summary>
        public IResponse MessageObject;
        
        public IMessage GetMessageObject()
        {
            return this.MessageObject;
        }
    }
    
    /// <summary>
    /// NetClient 网络纤程通知主纤程外网网关 Session 已断开销毁的消息。
    /// </summary>
    [Message]
    public class NetClient2Main_SessionDispose: MessageObject, IMessage
    {
        public static NetClient2Main_SessionDispose Create()
        {
            return ObjectPool.Fetch(typeof(NetClient2Main_SessionDispose)) as NetClient2Main_SessionDispose;
        }

        public override void Dispose()
        {
            ObjectPool.Recycle(this);
        }
        
        /// <summary>Session 断开时的错误码</summary>
        public int Error { get; set; }
    }
}
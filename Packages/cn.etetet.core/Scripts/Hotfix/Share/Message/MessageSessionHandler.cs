using System;

namespace ET
{
    /// <summary>
    /// 基于网络会话（<see cref="Session"/>）的单向消息处理器抽象基类（无需返回响应消息）。
    /// 用于处理客户端与服务端之间通过外网连接直接发送的通知型消息（如移动同步包、聊天通知等）。
    /// </summary>
    /// <typeparam name="Message">要处理的消息具体类型，必须继承自 <see cref="MessageObject"/>。</typeparam>
    public abstract class MessageSessionHandler<Message>: HandlerObject, IMessageSessionHandler where Message : MessageObject
    {
        /// <summary>
        /// 子类需实现的业务逻辑方法，处理收到的网络消息。
        /// </summary>
        /// <param name="session">接收到该消息的网络会话。</param>
        /// <param name="message">强类型的消息对象。</param>
        protected abstract ETTask Run(Session session, Message message);

        /// <summary>
        /// 实现 <see cref="IMessageSessionHandler.Handle"/> 接口，启动异步协程处理消息。
        /// </summary>
        /// <param name="session">接收到消息的网络会话。</param>
        /// <param name="msg">待处理的原始消息对象。</param>
        public void Handle(Session session, object msg)
        {
            HandleAsync(session, msg).Coroutine();
        }

        /// <summary>
        /// 内部异步处理流程：校验消息非空及会话连接状态后，调用子类的 <see cref="Run"/> 方法。
        /// </summary>
        private async ETTask HandleAsync(Session session, object message)
        {
            if (message == null)
            {
                Log.Error($"消息类型转换错误: {message.GetType().FullName} to {typeof (Message).Name}");
                return;
            }

            // 如果会话已经断开销毁，则丢弃消息不再处理
            if (session.IsDisposed)
            {
                Log.Error($"session disconnect {message}");
                return;
            }

            await this.Run(session, (Message)message);
        }

        /// <summary>
        /// 获取当前处理器负责的消息类型。
        /// </summary>
        public Type GetMessageType()
        {
            return typeof (Message);
        }

        /// <summary>
        /// 单向消息没有对应的返回响应类型，固定返回 null。
        /// </summary>
        public Type GetResponseType()
        {
            return null;
        }
    }
    
    /// <summary>
    /// 基于网络会话（<see cref="Session"/>）的 RPC 请求-响应消息处理器抽象基类。
    /// 自动管理响应对象（<typeparamref name="Response"/>）的对象池借出与回收、异常捕获、RpcId 回填及响应自动发送。
    /// </summary>
    /// <typeparam name="Request">RPC 请求消息类型，需实现 <see cref="IRequest"/>。</typeparam>
    /// <typeparam name="Response">RPC 响应消息类型，需实现 <see cref="IResponse"/>。</typeparam>
    public abstract class MessageSessionHandler<Request, Response>: HandlerObject, IMessageSessionHandler where Request : MessageObject, IRequest where Response : MessageObject, IResponse
    {
        /// <summary>
        /// 子类需实现的 RPC 业务逻辑方法，填充 <paramref name="response"/> 即可，框架会在方法结束后自动将响应发回对端。
        /// </summary>
        /// <param name="session">接收到该请求的网络会话。</param>
        /// <param name="request">客户端发来的强类型请求消息。</param>
        /// <param name="response">由框架从对象池分配好的响应对象，供子类填充返回数据或错误码。</param>
        protected abstract ETTask Run(Session session, Request request, Response response);

        /// <summary>
        /// 实现 <see cref="IMessageSessionHandler.Handle"/> 接口，启动异步协程处理 RPC 请求。
        /// </summary>
        /// <param name="session">接收到请求的网络会话。</param>
        /// <param name="message">待处理的原始请求对象。</param>
        public void Handle(Session session, object message)
        {
            HandleAsync(session, message).Coroutine();
        }

        /// <summary>
        /// 内部异步 RPC 处理流程：负责类型转换、对象池分配响应、异常捕获、跨 await 校验 Session 有效性并回发响应。
        /// </summary>
        private async ETTask HandleAsync(Session session, object message)
        {
            try
            {
                Request request = message as Request;
                if (request == null)
                {
                    throw new Exception($"消息类型转换错误: {message.GetType().FullName} to {typeof (Request).FullName}");
                }

                // 提前记录请求的 RpcId，用于后续原样回填给响应包以匹配客户端回调
                int rpcId = request.RpcId;
                long instanceId = session.InstanceId;

                // 使用 EntityRef 包装 Session，防止在 await Run(...) 异步挂起期间 Session 被销毁导致悬空引用
                EntityRef<Session> sessionRef = session;
                
                // 这里用using很安全，因为后面是session发送出去了
                using Response response = ObjectPool.Fetch<Response>();
                try
                {
                    await this.Run(session, request, response);
                }
                catch (RpcException exception)
                {
                    // 这里不能返回堆栈给客户端
                    Log.Error(exception.ToString());
                    response.Error = exception.Error;
                }
                catch (Exception exception)
                {
                    // 这里不能返回堆栈给客户端
                    Log.Error(exception.ToString());
                    response.Error = ErrorCode.ERR_RpcFail;
                }

                // 异步执行完毕后解引用 EntityRef，若 Session 已在期间断开销毁，这里会得到 null
                session = sessionRef;
                
                // 等回调回来,session可以已经断开了,所以需要判断session InstanceId是否一样
                if (session == null)
                {
                    return;
                }
                
                response.RpcId = rpcId; // 在这里设置rpcId是为了防止在Run中不小心修改rpcId字段
                session.Send(response);
            }
            catch (Exception e)
            {
                throw new Exception($"解释消息失败: {message.GetType().FullName}", e);
            }
        }

        /// <summary>
        /// 获取当前处理器负责的请求消息类型。
        /// </summary>
        public Type GetMessageType()
        {
            return typeof (Request);
        }

        /// <summary>
        /// 获取当前处理器对应的响应消息类型。
        /// </summary>
        public Type GetResponseType()
        {
            return typeof (Response);
        }
    }
}
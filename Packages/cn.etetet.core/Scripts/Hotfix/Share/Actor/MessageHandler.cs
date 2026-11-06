using System;

namespace ET
{
    /// <summary>
    /// Actor 实体单向消息处理器抽象基类（无需返回响应）。
    /// 用于处理直接通过 <see cref="FiberInstanceId"/> 投递给目标实体邮箱（<see cref="MailBoxComponent"/>）的单向通知消息。
    /// 注意：如果是需要通过 Location 服务定位的 <see cref="ILocationMessage"/>，应继承 Location 专用处理器而非本类。
    /// </summary>
    /// <typeparam name="E">挂载了邮箱的目标实体类型（如 <c>Scene</c>、<c>Unit</c>、<c>Player</c> 等）。</typeparam>
    /// <typeparam name="Message">要处理的 Actor 单向消息类型。</typeparam>
    public abstract class MessageHandler<E, Message>: HandlerObject, IMHandler where E : Entity where Message : class, IMessage
    {
        /// <summary>
        /// 子类需实现的业务逻辑方法，处理收到的 Actor 单向消息。
        /// </summary>
        /// <param name="entity">接收消息的目标实体。</param>
        /// <param name="message">收到的强类型消息对象。</param>
        protected abstract ETTask Run(E entity, Message message);

        /// <summary>
        /// 实现 <see cref="IMHandler.Handle"/> 接口：完成实体与消息的类型校验及转换后，调用子类的 <see cref="Run"/> 方法。
        /// </summary>
        /// <param name="entity">目标实体。</param>
        /// <param name="fromFiber">发送方纤程 ID。</param>
        /// <param name="actorMessage">收到的原始消息对象。</param>
        public async ETTask Handle(Entity entity, long fromFiber, MessageObject actorMessage)
        {
            if (actorMessage is not Message msg)
            {
                Log.Error($"消息类型转换错误: {actorMessage.GetType().FullName} to {typeof (Message).Name}");
                return;
            }

            if (entity is not E e)
            {
                Log.Error($"Actor类型转换错误: {entity.GetType().FullName} to {typeof (E).Name} --{typeof (Message).FullName}");
                return;
            }

            await this.Run(e, msg);
        }

        /// <summary>
        /// 获取当前处理器负责的请求消息类型，并在启动/分发时检查是否误将 Location 消息配置为了普通 Actor 处理器。
        /// </summary>
        public Type GetRequestType()
        {
            if (typeof (ILocationMessage).IsAssignableFrom(typeof (Message)))
            {
                Log.Error($"message is IActorLocationMessage but handler is AMActorHandler: {typeof (Message)}");
            }

            return typeof (Message);
        }

        /// <summary>
        /// 单向消息没有响应类型，固定返回 null。
        /// </summary>
        public Type GetResponseType()
        {
            return null;
        }
    }
    
    /// <summary>
    /// Actor 实体 RPC 请求-响应消息处理器抽象基类。
    /// 用于处理通过 <see cref="ProcessInnerSender"/> 发起的跨纤程/跨进程 Actor RPC 请求，执行完毕后自动通过 <see cref="ProcessInnerSender.Reply"/> 将响应发回来源纤程（<c>fromFiber</c>）。
    /// </summary>
    /// <typeparam name="E">挂载了邮箱的目标实体类型。</typeparam>
    /// <typeparam name="Request">Actor RPC 请求消息类型，需实现 <see cref="IRequest"/>。</typeparam>
    /// <typeparam name="Response">Actor RPC 响应消息类型，需实现 <see cref="IResponse"/>。</typeparam>
    public abstract class MessageHandler<E, Request, Response>: HandlerObject, IMHandler where E : Entity where Request : MessageObject, IRequest where Response : MessageObject, IResponse
    {
        /// <summary>
        /// 子类需实现的 Actor RPC 业务逻辑方法，填充 <paramref name="response"/> 即可，框架会在方法结束后自动将响应发回发送方纤程。
        /// </summary>
        /// <param name="unit">接收请求的目标实体。</param>
        /// <param name="request">收到的强类型请求消息。</param>
        /// <param name="response">从对象池分配的响应消息，供子类填充返回数据。</param>
        protected abstract ETTask Run(E unit, Request request, Response response);

        /// <summary>
        /// 实现 <see cref="IMHandler.Handle"/> 接口：负责类型校验、分配响应对象、捕获异常、回填 RpcId 并通过 <see cref="ProcessInnerSender"/> 回复发送方纤程。
        /// </summary>
        /// <param name="entity">目标实体。</param>
        /// <param name="fromFiber">发起请求的来源纤程 ID。</param>
        /// <param name="actorMessage">收到的原始请求消息对象。</param>
        public async ETTask Handle(Entity entity, long fromFiber, MessageObject actorMessage)
        {
            try
            {
                Fiber fiber = entity.Fiber();
                if (actorMessage is not Request request)
                {
                    Log.Error($"消息类型转换错误: {actorMessage.GetType().FullName} to {typeof (Request).Name}");
                    return;
                }

                if (entity is not E ee)
                {
                    Log.Error($"Actor类型转换错误: {entity.GetType().FullName} to {typeof (E).FullName} --{typeof (Request).FullName}");
                    return;
                }

                int rpcId = request.RpcId;
                Response response = ObjectPool.Fetch<Response>();
                try
                {
                    await this.Run(ee, request, response);
                }
                catch (RpcException exception)
                {
                    response.Error = exception.Error;
                    response.Message = exception.ToString();
                    Log.Error(exception);
                }
                catch (Exception exception)
                {
                    response.Error = ErrorCode.ERR_RpcFail;
                    response.Message = exception.ToString();
                    Log.Error(exception);
                }
                
                response.RpcId = rpcId;

                // 通过当前纤程根节点上的 ProcessInnerSender 将响应消息投递回发起请求的纤程
                fiber.Root.GetComponent<ProcessInnerSender>().Reply(fromFiber, response);
            }
            catch (Exception e)
            {
                throw new Exception($"解释消息失败: {actorMessage.GetType().FullName}", e);
            }
        }

        /// <summary>
        /// 获取当前处理器负责的请求消息类型，并检查是否误将 <see cref="ILocationRequest"/> 配置为了普通 Actor RPC 处理器。
        /// </summary>
        public Type GetRequestType()
        {
            if (typeof (ILocationRequest).IsAssignableFrom(typeof (Request)))
            {
                Log.Error($"message is IActorLocationMessage but handler is AMActorRpcHandler: {typeof (Request)}");
            }

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

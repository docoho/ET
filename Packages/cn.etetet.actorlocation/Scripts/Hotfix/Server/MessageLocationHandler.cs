using System;

namespace ET.Server
{
    /// <summary>
    /// 单向（实质带底层 ACK 确认）ActorLocation 消息处理器抽象基类（处理实现 <see cref="ILocationMessage"/> 的消息）。
    /// 注意：虽然业务层以 Send 语义调用，但底层仍然继承自 <see cref="ILocationRequest"/> 并立即回复一个空的 <see cref="MessageResponse"/>，以便发送方确认目标实体确实存在（若不存在则可感知 <see cref="ErrorCode.ERR_NotFoundActor"/> 并重查 Location 重试）。
    /// </summary>
    /// <typeparam name="E">接收消息的目标实体类型（如 Unit）</typeparam>
    /// <typeparam name="Message">消息类型</typeparam>
    public abstract class MessageLocationHandler<E, Message>: HandlerObject, IMHandler where E : Entity where Message : class, ILocationMessage
    {
        /// <summary>
        /// 供子类实现的业务逻辑处理方法。
        /// </summary>
        protected abstract ETTask Run(E entity, Message message);

        public async ETTask Handle(Entity entity, long fromFiber, MessageObject actorMessage)
        {
            Fiber fiber = entity.Fiber();
            if (actorMessage is not Message message)
            {
                Log.Error($"消息类型转换错误: {actorMessage.GetType().FullName} to {typeof (Message).Name}");
                return;
            }

            if (entity is not E e)
            {
                Log.Error($"Actor类型转换错误: {entity.GetType().FullName} to {typeof (E).FullName} --{typeof (Message).FullName}");
                return;
            }
            
            MessageResponse response = ObjectPool.Fetch<MessageResponse>();
            response.RpcId = message.RpcId;
            fiber.Root.GetComponent<ProcessInnerSender>().Reply(fromFiber, response);

            await this.Run(e, message);
        }

        public Type GetRequestType()
        {
            return typeof (Message);
        }

        public Type GetResponseType()
        {
            return typeof (MessageResponse);
        }
    }
    
    
    
    /// <summary>
    /// 双向 RPC ActorLocation 请求-响应处理器抽象基类（处理实现 <see cref="ILocationRequest"/> / <see cref="ILocationResponse"/> 的消息）。
    /// 在执行完子类的 <see cref="Run"/> 方法后自动将 <typeparamref name="Response"/> 回发给调用方。
    /// </summary>
    /// <typeparam name="E">接收请求的目标实体类型</typeparam>
    /// <typeparam name="Request">请求消息类型</typeparam>
    /// <typeparam name="Response">响应消息类型</typeparam>
    public abstract class MessageLocationHandler<E, Request, Response>: HandlerObject, IMHandler where E : Entity where Request : MessageObject, ILocationRequest where Response : MessageObject, ILocationResponse
    {
        /// <summary>
        /// 供子类实现的业务请求处理方法。
        /// </summary>
        protected abstract ETTask Run(E unit, Request request, Response response);

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

                //long entityId = entity.Id;

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
                }
                catch (Exception exception)
                {
                    response.Error = ErrorCode.ERR_RpcFail;
                    response.Message = exception.ToString();
                }
                response.RpcId = rpcId;
                
                // 这里是为了保证response消息在handler消息处理完成之后发出，
                // 因为MessageLocationSenderComponentSystem里面的Send方法有可能需要从location获取actorid
                // 这样会导致send实际上进入了新的协程，从而response却先发送出去了
                //using (await fiber.Root.CoroutineLockComponent.Wait(CoroutineLockType.MessageLocationSender, entityId))
                //{
                // map发消息改成了用ActorId发送，这样可以不用加协程锁了
                fiber.Root.GetComponent<ProcessInnerSender>().Reply(fromFiber, response);
                //}
            }
            catch (Exception e)
            {
                throw new Exception($"解释消息失败: {actorMessage.GetType().FullName}", e);
            }
        }

        public Type GetRequestType()
        {
            return typeof (Request);
        }

        public Type GetResponseType()
        {
            return typeof (Response);
        }
    }
}

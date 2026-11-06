using System;
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 单个 Actor 消息处理器的注册条目信息（包含限定生效的 <see cref="SceneType"/> 与处理器实例 <see cref="IMHandler"/>）。
    /// </summary>
    public struct MessageDispatcherInfo
    {
        /// <summary>
        /// 该处理器绑定的目标场景类型（<see cref="ET.SceneType"/>）。
        /// </summary>
        public int SceneType { get; }
        
        /// <summary>
        /// 处理器实例。
        /// </summary>
        public IMHandler IMHandler { get; }

        public MessageDispatcherInfo(int sceneType, IMHandler imHandler)
        {
            this.SceneType = sceneType;
            this.IMHandler = imHandler;
        }
    }
    
    /// <summary>
    /// Actor消息分发组件（全局只读单例）。
    /// <remarks>
    /// 在启动或热重载时扫描所有带有 <see cref="MessageHandlerAttribute"/> 的类，按消息 <see cref="Type"/> 归类注册；
    /// 当某个挂载了 <see cref="MailBoxComponent"/> 的实体收到 Actor 消息时，由邮箱处理器调用 <see cref="Handle"/> 或 <see cref="HandleAsync"/>，
    /// 根据实体所属场景的 <c>SceneType</c> 匹配对应的 <see cref="IMHandler"/> 执行业务逻辑。
    /// </remarks>
    /// </summary>
    [CodeProcess]
    [AllowInstance]
    public class MessageDispatcher: Singleton<MessageDispatcher>, ISingletonAwake
    {
        /// <summary>
        /// 消息类型到处理器列表的映射表（同一消息可以在不同 <see cref="SceneType"/> 下注册不同的处理器）。
        /// </summary>
        private readonly Dictionary<Type, List<MessageDispatcherInfo>> messageHandlers = new();

        /// <summary>
        /// 扫描并注册所有标记了 <see cref="MessageHandlerAttribute"/> 的 Actor 消息处理器。
        /// </summary>
        public void Awake()
        {
            HashSet<Type> types = CodeTypes.Instance.GetTypes(typeof (MessageHandlerAttribute));
            
            foreach (Type type in types)
            {
                this.Register(type);
            }
        }
        
        /// <summary>
        /// 实例化单个处理器类，校验其请求-响应类型配对，并注册到分发字典中。
        /// </summary>
        private void Register(Type type)
        {
            object obj = Activator.CreateInstance(type);

            IMHandler imHandler = obj as IMHandler;
            if (imHandler == null)
            {
                throw new Exception($"message handler not inherit IMActorHandler abstract class: {obj.GetType().FullName}");
            }
                
            object[] attrs = type.GetCustomAttributes(typeof(MessageHandlerAttribute), true);

            foreach (object attr in attrs)
            {
                MessageHandlerAttribute messageHandlerAttribute = attr as MessageHandlerAttribute;

                Type messageType = imHandler.GetRequestType();

                Type handleResponseType = imHandler.GetResponseType();
                if (handleResponseType != null)
                {
                    Type responseType = OpcodeType.Instance.GetResponseType(messageType);
                    if (handleResponseType != responseType)
                    {
                        throw new Exception($"message handler response type error: {messageType.FullName}");
                    }
                }

                MessageDispatcherInfo messageDispatcherInfo = new(messageHandlerAttribute.SceneType, imHandler);

                this.RegisterHandler(messageType, messageDispatcherInfo);
            }
        }
        
        private void RegisterHandler(Type type, MessageDispatcherInfo handler)
        {
            if (!this.messageHandlers.ContainsKey(type))
            {
                this.messageHandlers.Add(type, new List<MessageDispatcherInfo>());
            }

            this.messageHandlers[type].Add(handler);
        }

        /// <summary>
        /// 异步串行分发并等待所有匹配当前实体 <c>SceneType</c> 的处理器执行完毕（使用 <see cref="EntityRef{T}"/> 校验实体在 await 期间是否已销毁）。
        /// </summary>
        public async ETTask HandleAsync(Entity entity, long fromFiber, MessageObject message)
        {
            List<MessageDispatcherInfo> list;
            if (!this.messageHandlers.TryGetValue(message.GetType(), out list))
            {
                throw new Exception($"not found message handler: {message} {entity.GetType().FullName}");
            }
            
            EntityRef<Entity> entityRef = entity;
            int sceneType = entity.IScene.SceneType;
            foreach (MessageDispatcherInfo actorMessageDispatcherInfo in list)
            {
                if (!SceneTypeSingleton.IsSame(actorMessageDispatcherInfo.SceneType, sceneType))
                {
                    continue;
                }

                entity = entityRef;
                await actorMessageDispatcherInfo.IMHandler.Handle(entity, fromFiber, message);   
            }
        }
        
        /// <summary>
        /// 并发启动所有匹配当前实体 <c>SceneType</c> 的消息处理器协程（不等待完成，常用于无序邮箱 <see cref="MailBoxType.UnOrderedMessage"/>）。
        /// </summary>
        public void Handle(Entity entity, long fromFiber, MessageObject message)
        {
            List<MessageDispatcherInfo> list;
            if (!this.messageHandlers.TryGetValue(message.GetType(), out list))
            {
                throw new Exception($"not found message handler: {message} {entity.GetType().FullName}");
            }
            
            int sceneType = entity.IScene.SceneType;
            foreach (MessageDispatcherInfo actorMessageDispatcherInfo in list)
            {
                if (!SceneTypeSingleton.IsSame(actorMessageDispatcherInfo.SceneType, sceneType))
                {
                    continue;
                }
                actorMessageDispatcherInfo.IMHandler.Handle(entity, fromFiber, message).Coroutine();   
            }
        }
    }
}

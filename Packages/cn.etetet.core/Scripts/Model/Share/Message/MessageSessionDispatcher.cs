using System.Collections.Generic;
using System;

namespace ET
{
    /// <summary>
    /// 单个 Session 消息处理器的注册条目信息（包含限定生效的 <see cref="SceneType"/> 与处理器实例 <see cref="IMessageSessionHandler"/>）。
    /// </summary>
    public struct MessageSessionDispatcherInfo
    {
        public int SceneType { get; }
        public IMessageSessionHandler IMHandler { get; }

        public MessageSessionDispatcherInfo(int sceneType, IMessageSessionHandler imHandler)
        {
            this.SceneType = sceneType;
            this.IMHandler = imHandler;
        }
    }
    
    /// <summary>
    /// Session 网络消息分发器全局只读单例（负责客户端与服务端直连会话 <see cref="Session"/> 上的协议分发）。
    /// <remarks>
    /// 在启动时扫描所有标记了 <see cref="MessageSessionHandlerAttribute"/> 的处理器类，按消息的 16 位 <c>Opcode</c> 归类注册；
    /// 当 <see cref="NetComponent"/> 从某个 <see cref="Session"/> 收到非 Response 消息时，调用 <see cref="Handle"/> 匹配当前场景类型并执行对应的处理器。
    /// </remarks>
    /// </summary>
    [CodeProcess]
    [AllowInstance]
    public class MessageSessionDispatcher: Singleton<MessageSessionDispatcher>, ISingletonAwake
    {
        /// <summary>
        /// 协议号（Opcode）到 Session 消息处理器列表的映射表。
        /// </summary>
        private readonly Dictionary<ushort, List<MessageSessionDispatcherInfo>> handlers = new();
        
        /// <summary>
        /// 扫描并注册所有标记了 <see cref="MessageSessionHandlerAttribute"/> 的会话消息处理器。
        /// </summary>
        public void Awake()
        {
            HashSet<Type> types = CodeTypes.Instance.GetTypes(typeof (MessageSessionHandlerAttribute));

            foreach (Type type in types)
            {
                IMessageSessionHandler iMessageSessionHandler = Activator.CreateInstance(type) as IMessageSessionHandler;
                if (iMessageSessionHandler == null)
                {
                    Log.Error($"message handle {type.Name} 需要继承 IMHandler");
                    continue;
                }

                object[] attrs = type.GetCustomAttributes(typeof(MessageSessionHandlerAttribute), true);
                
                foreach (object attr in attrs)
                {
                    MessageSessionHandlerAttribute messageSessionHandlerAttribute = attr as MessageSessionHandlerAttribute;
                    
                    Type messageType = iMessageSessionHandler.GetMessageType();
                    
                    ushort opcode = OpcodeType.Instance.GetOpcode(messageType);
                    if (opcode == 0)
                    {
                        Log.Error($"消息opcode为0: {messageType.Name}");
                        continue;
                    }

                    MessageSessionDispatcherInfo messageSessionDispatcherInfo = new (messageSessionHandlerAttribute.SceneType, iMessageSessionHandler);
                    this.RegisterHandler(opcode, messageSessionDispatcherInfo);
                }
            }
        }
        
        private void RegisterHandler(ushort opcode, MessageSessionDispatcherInfo handler)
        {
            if (!this.handlers.ContainsKey(opcode))
            {
                this.handlers.Add(opcode, new List<MessageSessionDispatcherInfo>());
            }

            this.handlers[opcode].Add(handler);
        }

        /// <summary>
        /// 根据消息的 Opcode 查找并调用所有匹配当前 <paramref name="session"/> 所属 <c>SceneType</c> 的处理器。
        /// </summary>
        public void Handle(Session session, object message)
        {
            List<MessageSessionDispatcherInfo> actions;
            ushort opcode = OpcodeType.Instance.GetOpcode(message.GetType());
            if (!this.handlers.TryGetValue(opcode, out actions))
            {
                Log.Error($"消息没有处理: {opcode} {message}");
                return;
            }

            int sceneType = session.IScene.SceneType;
            foreach (MessageSessionDispatcherInfo ev in actions)
            {
                if (!SceneTypeSingleton.IsSame(ev.SceneType, sceneType))
                {
                    continue;
                }
                
                try
                {
                    ev.IMHandler.Handle(session, message);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }
    }
}

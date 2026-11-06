using System;
using System.Net;
using System.Net.Sockets;

namespace ET
{
    /// <summary>
    /// 外网网络组件系统（<see cref="NetComponent"/> 的逻辑扩展）。
    /// 负责驱动底层网络服务（<see cref="KService"/>），管理网络会话（<see cref="Session"/>）的创建、接收连接、数据读取反序列化及异常处理。
    /// </summary>
    [EntitySystemOf(typeof(NetComponent))]
    public static partial class NetComponentSystem
    {
        /// <summary>
        /// 初始化外网网络组件，创建基于 KCP 传输层的 <see cref="KService"/> 并绑定连接、读取、错误回调。
        /// </summary>
        /// <param name="self">当前网络组件实例。</param>
        /// <param name="kcpTransport">底层传输实现（如 UDP 或 TCP 模拟传输）。</param>
        [EntitySystem]
        private static void Awake(this NetComponent self, IKcpTransport kcpTransport)
        {
            self.AService = new KService(kcpTransport, ServiceType.Outer);
            self.AService.AcceptCallback = self.OnAccept;
            self.AService.ReadCallback = self.OnRead;
            self.AService.ErrorCallback = self.OnError;
        }
        
        /// <summary>
        /// 每帧驱动底层网络服务更新（收发包轮询、KCP 时钟更新等）。
        /// </summary>
        [EntitySystem]
        private static void Update(this NetComponent self)
        {
            self.AService.Update();
        }

        /// <summary>
        /// 组件销毁时释放底层网络服务资源。
        /// </summary>
        [EntitySystem]
        private static void Destroy(this NetComponent self)
        {
            self.AService.Dispose();
        }

        /// <summary>
        /// 底层通道发生错误时的回调：查找对应的 <see cref="Session"/>，记录错误码并销毁该会话。
        /// </summary>
        private static void OnError(this NetComponent self, long channelId, int error)
        {
            Session session = self.GetChild<Session>(channelId);
            if (session == null)
            {
                return;
            }

            session.Error = error;
            session.Dispose();
        }

        // 只有服务端会accept
        // 这个channelId是由CreateAcceptChannelId生成的
        /// <summary>
        /// 服务端接收到新的客户端连接请求时的回调：创建对应的 <see cref="Session"/> 子实体，并挂载安全防护组件。
        /// </summary>
        private static void OnAccept(this NetComponent self, long channelId, IPEndPoint ipEndPoint)
        {
            Session session = self.AddChildWithId<Session, AService>(channelId, self.AService);
            session.RemoteAddress = ipEndPoint;
            
            // 挂上这个组件，5秒就会删除session，所以客户端验证完成要删除这个组件。该组件的作用就是防止外挂一直连接不发消息也不进行权限验证
            session.AddComponent<SessionAcceptTimeoutComponent>();
            // 客户端连接，2秒检查一次recv消息，10秒没有消息则断开
            session.AddComponent<SessionIdleCheckerComponent>();
            // 挂载消息频率统计组件，防止客户端恶意刷包
            session.AddComponent<MessageStatisticsComponent>();
        }
        
        /// <summary>
        /// 底层网络通道读取到完整数据包时的回调：反序列化消息、校验外网协议号范围，并通过事件系统分发给当前场景处理。
        /// </summary>
        private static void OnRead(this NetComponent self, long channelId, MemoryBuffer memoryBuffer)
        {
            Session session = self.GetChild<Session>(channelId);
            if (session == null)
            {
                return;
            }
            // 更新会话的最后收到消息时间戳，用于空闲心跳检测
            session.LastRecvTime = self.GetSingleton<TimeInfo>().ClientNow();
            
            // 将二进制缓冲区反序列化为消息对象，并回收 MemoryBuffer
            (FiberInstanceId _, object message) = MessageSerializeHelper.ToMessage(self.AService, memoryBuffer);
            self.AService.Recycle(memoryBuffer);

            // 外网消息是10000~20000（防止客户端伪造内网消息号越权调用服务端内部接口）
            ushort opcode = OpcodeType.Instance.GetOpcode(message.GetType());
            if (opcode is > 20000 or < 10000)
            {
                Log.Error($"client message must in (10000, 20000), opcode: {opcode}");
                return;
            }

            // 机器人消息只在Test进程中处理
            if (message is ITestMessage)
            {
                if (Options.Instance.SceneName != "Test")
                {
                    Log.Error($"Test message received in non-Test scene: {message.GetType().Name}");
                    return;
                }
            }
            
            LogMsg.Instance.Recv(self.Fiber(), message);

            // 根据当前所在的 SceneType 触发对应的 NetComponentOnRead 处理器（如客户端、Gate 网关、Router 等各自有不同的分发策略）
            EventSystem.Instance.Invoke(self.IScene.SceneType, new NetComponentOnRead() {Session = session, Message = message});
        }
        
        /// <summary>
        /// 客户端主动发起连接：随机生成唯一通道 ID，创建 <see cref="Session"/> 并连接目标服务端地址。
        /// </summary>
        /// <param name="self">当前网络组件实例。</param>
        /// <param name="realIPEndPoint">目标服务端的真实 IP 与端口。</param>
        /// <returns>新建的网络会话实例。</returns>
        public static Session Create(this NetComponent self, IPEndPoint realIPEndPoint)
        {
            int channelId;
            while (true)
            {
                channelId = RandomGenerator.RandInt32();
                if (self.GetChild<Session>(channelId) == null)
                {
                    break;
                }
            }
            Session session = self.AddChildWithId<Session, AService>(channelId, self.AService);
            session.RemoteAddress = realIPEndPoint;
            session.AddComponent<SessionIdleCheckerComponent>();
            
            self.AService.Create(session.Id, realIPEndPoint);

            return session;
        }

        /// <summary>
        /// 通过路由节点（Router）创建连接：使用指定的本地连接号（<paramref name="localConn"/>）作为通道 ID，底层连接到路由地址。
        /// </summary>
        /// <param name="self">当前网络组件实例。</param>
        /// <param name="routerIPEndPoint">中间路由节点（Router）的 IP 与端口。</param>
        /// <param name="realIPEndPoint">最终目标服务端（如 Gate）的真实 IP 与端口。</param>
        /// <param name="localConn">路由握手协商好的本地连接 ID。</param>
        /// <returns>新建的网络会话实例。</returns>
        public static Session Create(this NetComponent self, IPEndPoint routerIPEndPoint, IPEndPoint realIPEndPoint, uint localConn)
        {
            long channelId = localConn;
            Session session = self.AddChildWithId<Session, AService>(channelId, self.AService);
            session.RemoteAddress = realIPEndPoint;
            session.AddComponent<SessionIdleCheckerComponent>();
            self.AService.Create(session.Id, routerIPEndPoint);
            return session;
        }

        /// <summary>
        /// 获取底层传输服务绑定的本地网络端点（IP 与端口）。
        /// </summary>
        public static IPEndPoint GetBindPoint(this NetComponent self)
        {
            return self.AService.GetBindPoint();
        }
    }
}
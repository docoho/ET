using System.Net;
using System.Net.Sockets;

namespace ET
{
    /// <summary>
    /// <see cref="NetComponent"/> 收到非 RPC 响应的普通会话消息时发布的事件参数结构体（支持业务层通过 <c>[Event]</c> 监听并自定义网关路由转发）。
    /// </summary>
    public struct NetComponentOnRead
    {
        /// <summary>
        /// 收到该消息的会话实体引用。
        /// </summary>
        public EntityRef<Session> Session { get; set; }
        /// <summary>
        /// 反序列化后的消息对象。
        /// </summary>
        public object Message { get; set; }
    }
    
    /// <summary>
    /// 外网网络服务组件，挂载在客户端或网关的 <see cref="Scene"/> 上。
    /// <remarks>
    /// 内部持有一个底层网络服务 <see cref="AService"/>（默认使用 <see cref="KService"/>），
    /// 并以子实体（Child）的形式管理该端口上所有的客户端连接会话 <see cref="Session"/>（其中 <c>Session.Id == ChannelId</c>）。
    /// </remarks>
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class NetComponent: Entity, IAwake<IKcpTransport>, IDestroy, IUpdate
    {
        /// <summary>
        /// 底层网络传输服务实例（如 <see cref="KService"/>）。
        /// </summary>
        public AService AService { get; set; }
    }
}
using System.Net;

namespace ET.Server
{
    /// <summary>
    /// 软路由节点连接状态枚举。
    /// </summary>
    public enum RouterStatus
    {
        /// <summary>正在握手同步阶段（尚未完成与内网 Gate/Realm 的连接确认）。</summary>
        Sync,
        /// <summary>握手完成，处于正常双向数据包（MSG）转发阶段。</summary>
        Msg,
    }

    /// <summary>
    /// 软路由上的单个客户端连接映射节点（作为 <see cref="RouterComponent"/> 的子实体，其实体 <c>Id</c> 即为客户端的 <see cref="OuterConn"/>）。
    /// <para>维护客户端外网终结点（<see cref="OuterIpEndPoint"/>）与后端内网服务终结点（<see cref="InnerIpEndPoint"/>）以及内外网连接号（<see cref="OuterConn"/> / <see cref="InnerConn"/>）的映射关系。</para>
    /// </summary>
    [ChildOf(typeof(RouterComponent))]
    public class RouterNode: Entity, IDestroy, IAwake
    {
        /// <summary>目标内网服务（Realm/Gate）的地址字符串。</summary>
        public string InnerAddress;
        /// <summary>目标内网服务的 <see cref="IPEndPoint"/>。</summary>
        public IPEndPoint InnerIpEndPoint;
        /// <summary>客户端外网最新的 <see cref="IPEndPoint"/>。</summary>
        public IPEndPoint OuterIpEndPoint;
        /// <summary>该客户端当前使用的外网传输层通道（UDP 或 TCP）。</summary>
        public IKcpTransport KcpTransport;

        /// <summary>外网客户端侧 KCP 连接 ID（直接复用当前实体的 <c>Id</c>）。</summary>
        public uint OuterConn
        {
            get
            {
                return (uint)this.Id;
            }
        }
        /// <summary>内网服务端（Gate/Realm）分配的 KCP 连接 ID。</summary>
        public uint InnerConn;
        /// <summary>客户端每次发起连接生成的随机标识 ID（用于区分同一客户端的不同连接尝试）。</summary>
        public uint ConnectId;
        /// <summary>最后一次收到外网客户端报文的时间戳。</summary>
        public long LastRecvOuterTime;
        /// <summary>最后一次收到内网服务端报文的时间戳。</summary>
        public long LastRecvInnerTime;

        /// <summary>累计收到的 RouterSYN / RouterReconnectSYN 握手次数（防刷限流）。</summary>
        public int RouterSyncCount;
        /// <summary>累计收到的 KCP SYN 握手次数（防刷限流）。</summary>
        public int SyncCount;

#region 限制外网消息数量，一秒最多50个包

        /// <summary>限流统计窗口的起始时间戳。</summary>
        public long LastCheckTime;
        /// <summary>当前秒窗口内已收到的外网数据包计数。</summary>
        public int LimitCountPerSecond;

#endregion

        /// <summary>当前路由节点的握手/转发状态。</summary>
        public RouterStatus Status;
    }
}
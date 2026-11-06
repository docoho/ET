using System;
using System.Collections.Generic;
using System.Net;

namespace ET.Server
{
    /// <summary>
    /// 跨进程内网 Socket 通信管理组件（仅挂载在每个进程的 <c>NetInner</c> 纤程根场景上）。
    /// <para>持有本进程唯一的内网网络服务 <see cref="AService"/>，并按远端进程地址 <see cref="Address"/> 维护内网 <see cref="Session"/> 缓存字典。</para>
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ProcessOuterSender: Entity, IAwake<IPEndPoint>, IUpdate, IDestroy
    {
        /// <summary>跨进程 RPC 默认超时时间（40 秒）。</summary>
        public const long TIMEOUT_TIME = 40 * 1000;
        
        /// <summary>跨进程 RPC 自增请求流水号。</summary>
        public int RpcId;

        /// <summary>按 <see cref="RpcId"/> 索引的跨进程 RPC 响应等待回调表。</summary>
        public readonly Dictionary<int, MessageSenderStruct> requestCallback = new();
        
        /// <summary>底层内网传输服务（默认使用 KCP，可切换为 TCP）。</summary>
        public AService AService;
        
        /// <summary>内网通信使用的传输层协议类型。</summary>
        public NetworkProtocol InnerProtocol = NetworkProtocol.KCP;

        public short channelIdCout;

        /// <summary>远端进程内网地址（<see cref="Address"/>）到对应内网连接 <see cref="Session"/> 的映射表。</summary>
        public Dictionary<Address, EntityRef<Session>> AddressSessions = new();
    }
}
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace ET
{
    /// <summary>
    /// 传输层网络协议类型枚举。
    /// </summary>
    public enum NetworkProtocol
    {
        /// <summary>
        /// 标准面向连接字节流协议 TCP。
        /// </summary>
        TCP,
        /// <summary>
        /// 基于 UDP 的快速可靠 ARQ 协议 KCP。
        /// </summary>
        KCP,
        /// <summary>
        /// 面向浏览器/小游戏环境的 WebSocket 协议。
        /// </summary>
        Websocket,
        /// <summary>
        /// 原始无连接数据报协议 UDP。
        /// </summary>
        UDP,
    }
}
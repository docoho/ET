using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace ET.Server
{
    
    /// <summary>
    /// 软路由核心组件（挂载在 <c>SceneType.Router</c> 场景上）。
    /// <para>同时持有面向公网客户端的 UDP/TCP 传输对象（<see cref="OuterUdp"/>、<see cref="OuterTcp"/>）和面向内网后端节点的 UDP 传输对象（<see cref="InnerSocket"/>），以子实体形式管理所有活跃的 <see cref="RouterNode"/>。</para>
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class RouterComponent: Entity, IAwake<IPEndPoint, string>, IDestroy, IUpdate
    {
        /// <summary>面向外网客户端的 UDP 传输通道。</summary>
        public IKcpTransport OuterUdp;
        /// <summary>面向外网客户端的 TCP（或 WebSocket）传输通道。</summary>
        public IKcpTransport OuterTcp;
        /// <summary>面向内网 Gate/Realm 的 UDP 传输通道。</summary>
        public IKcpTransport InnerSocket;
        /// <summary>用于接收报文源地址的复用 <see cref="EndPoint"/>。</summary>
        public EndPoint IPEndPoint = new IPEndPoint(IPAddress.Any, 0);

        /// <summary>单包收发转发复用的字节缓冲区（MTU 1500 字节）。</summary>
        public byte[] Cache = new byte[1500];

        /// <summary>待轮询检查连接超时的 <see cref="RouterNode"/>（以 <c>OuterConn</c> 为 ID）队列。</summary>
        public Queue<uint> checkTimeout = new();

        /// <summary>上一次执行超时检查的时间戳（毫秒）。</summary>
        public long LastCheckTime = 0;
    }
}
using System;
using System.IO;
using System.Net;

namespace ET
{
    /// <summary>
    /// 网络通道的连接方向类型枚举。
    /// </summary>
    public enum ChannelType
    {
        /// <summary>
        /// 主动发起连接的客户端通道。
        /// </summary>
        Connect,
        /// <summary>
        /// 服务端被动接受连接的通道。
        /// </summary>
        Accept,
    }

    /// <summary>
    /// 网络数据包协议头常量与解包结构定义。
    /// </summary>
    public struct Packet
    {
        /// <summary>
        /// 最小合法数据包字节长度（至少包含 2 字节 Opcode）。
        /// </summary>
        public const int MinPacketSize = 2;
        /// <summary>
        /// 协议号（Opcode）占用的字节长度（<c>ushort</c> = 2 字节）。
        /// </summary>
        public const int OpcodeLength = 2;
        /// <summary>
        /// 内网消息头中目标 <see cref="FiberInstanceId"/> 的起始偏移。
        /// </summary>
        public const int FiberInstanceIdIndex = 0;
        /// <summary>
        /// 内网消息头中目标 <see cref="FiberInstanceId"/> 占用的字节长度（8 字节 Fiber + 8 字节 InstanceId = 16 字节）。
        /// </summary>
        public const int FiberInstanceIdLength = 16;

        /// <summary>
        /// 消息协议号（对应消息类型）。
        /// </summary>
        public ushort Opcode;
        /// <summary>
        /// 目标 ActorId。
        /// </summary>
        public long ActorId;
        /// <summary>
        /// 消息体载荷内存流。
        /// </summary>
        public MemoryStream MemoryStream;
    }

    /// <summary>
    /// 底层网络连接通道（Channel）抽象基类，代表一条具体的点对点传输链路（如 <see cref="KChannel"/> 或 <see cref="TChannel"/>）。
    /// </summary>
    public abstract class AChannel: IDisposable
    {
        /// <summary>
        /// 通道唯一标识 Id（与上层 <c>Session.Id</c> 保持一致；置为 0 表示通道已销毁）。
        /// </summary>
        public long Id;
		
        /// <summary>
        /// 通道连接类型（主动连接 <see cref="ChannelType.Connect"/> 或被动接受 <see cref="ChannelType.Accept"/>）。
        /// </summary>
        public ChannelType ChannelType { get; protected set; }

        /// <summary>
        /// 通道最近一次发生的错误码（0 表示正常，非 0 参见 <see cref="ErrorCore"/>）。
        /// </summary>
        public int Error { get; set; }
		
        private IPEndPoint remoteAddress;

        /// <summary>
        /// 对端网络地址（IP + 端口）。
        /// </summary>
        public IPEndPoint RemoteAddress
        {
            get
            {
                return this.remoteAddress;
            }
            set
            {
                this.remoteAddress = value;
            }
        }

        /// <summary>
        /// 判断当前通道是否已被释放销毁（<c>Id == 0</c>）。
        /// </summary>
        public bool IsDisposed
        {
            get
            {
                return this.Id == 0;	
            }
        }

        /// <summary>
        /// 关闭并释放当前网络通道资源。
        /// </summary>
        public abstract void Dispose();
    }
}

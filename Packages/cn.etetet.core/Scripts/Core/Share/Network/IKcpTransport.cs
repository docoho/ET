using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace ET
{
    /// <summary>
    /// KCP 协议的底层传输层抽象接口。
    /// <remarks>
    /// KCP 本身是一个纯算法协议，不绑定具体的底层传输介质；
    /// ET 通过此接口允许 <see cref="KService"/> 无缝切换运行在标准 UDP（<see cref="UdpTransport"/>）或伪装为 TCP 承载（<see cref="TcpTransport"/>，用于部分禁用 UDP 的特殊网络环境）之上。
    /// </remarks>
    /// </summary>
    public interface IKcpTransport: IDisposable
    {
        /// <summary>
        /// 向指定远端地址发送原始 KCP 数据包。
        /// </summary>
        void Send(byte[] bytes, int index, int length, EndPoint endPoint, ChannelType channelType);
        /// <summary>
        /// 从传输层接收一个原始数据包并填充来源地址 <paramref name="endPoint"/>。
        /// </summary>
        int Recv(byte[] buffer, ref EndPoint endPoint);
        /// <summary>
        /// 获取传输层绑定的本地监听地址。
        /// </summary>
        IPEndPoint GetBindPoint();
        /// <summary>
        /// 检查传输层当前是否有可读数据包到达。
        /// </summary>
        bool Available();
        /// <summary>
        /// 驱动传输层自身的状态轮询。
        /// </summary>
        void Update();
        /// <summary>
        /// 当上层 KCP 通道报错或断开时通知传输层清理关联状态。
        /// </summary>
        void OnError(long id, int error);
    }

    /// <summary>
    /// 基于无连接 UDP Socket 的标准 KCP 传输层实现。
    /// </summary>
    public class UdpTransport: IKcpTransport
    {
        private readonly Socket socket;

        /// <summary>
        /// 创建不绑定固定端口的客户端 UDP 传输层。
        /// </summary>
        public UdpTransport(AddressFamily addressFamily)
        {
            this.socket = new Socket(addressFamily, SocketType.Dgram, ProtocolType.Udp);
            NetworkHelper.SetSioUdpConnReset(this.socket);
        }
        
        /// <summary>
        /// 创建绑定指定本地地址与端口的服务端 UDP 传输层（非 macOS 平台下默认设置 64MB 收发缓冲区以应对高并发吞吐）。
        /// </summary>
        public UdpTransport(IPEndPoint ipEndPoint)
        {
            this.socket = new Socket(ipEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                this.socket.SendBufferSize = Kcp.OneM * 64;
                this.socket.ReceiveBufferSize = Kcp.OneM * 64;
            }
            
            try
            {
                this.socket.Bind(ipEndPoint);
            }
            catch (Exception e)
            {
                throw new Exception($"bind error: {ipEndPoint}", e);
            }

            NetworkHelper.SetSioUdpConnReset(this.socket);
        }
        
        public void Send(byte[] bytes, int index, int length, EndPoint endPoint, ChannelType channelType)
        {
            this.socket.SendTo(bytes, index, length, SocketFlags.None, endPoint);
        }
        
        public int Recv(byte[] buffer, ref EndPoint endPoint)
        {
            return this.socket.ReceiveFrom(buffer, ref endPoint);
        }

        public IPEndPoint GetBindPoint()
        {
            return this.socket.LocalEndPoint as IPEndPoint;
        }

        public bool Available()
        {
            return this.socket.Poll(0, SelectMode.SelectRead);
        }

        public void Update()
        {
        }

        public void OnError(long id, int error)
        {
        }

        public void Dispose()
        {
            this.socket?.Dispose();
        }
    }

    /// <summary>
    /// 基于 <see cref="TService"/>（TCP）模拟数据报收发的 KCP 传输层实现（支持 KCP over TCP）。
    /// </summary>
    public class TcpTransport: IKcpTransport
    {
        private readonly TService tService;

        /// <summary>
        /// TCP 通道 Id 与远端 <see cref="EndPoint"/> 的双向映射表。
        /// </summary>
        private readonly DoubleMap<long, EndPoint> idEndpoints = new();

        /// <summary>
        /// 已从底层 TCP 通道接收、等待上层 <see cref="KService"/> 读取的数据包队列。
        /// </summary>
        private readonly Queue<(EndPoint, MemoryBuffer)> channelRecvDatas = new();

        /// <summary>
        /// 记录每个 TCP 通道最近一次读写活跃时间戳（用于 30 秒空闲超时清理）。
        /// </summary>
        private readonly Dictionary<long, long> readWriteTime = new();

        /// <summary>
        /// 待轮询检查超时的 TCP 通道 Id 队列。
        /// </summary>
        private readonly Queue<long> channelIds = new();
        
        public TcpTransport(AddressFamily addressFamily)
        {
            this.tService = new TService(addressFamily, ServiceType.Outer);
            this.tService.ErrorCallback = this.OnError;
            this.tService.ReadCallback = this.OnRead;
        }
        
        public TcpTransport(IPEndPoint ipEndPoint)
        {
            this.tService = new TService(ipEndPoint, ServiceType.Outer);
            this.tService.AcceptCallback = this.OnAccept;
            this.tService.ErrorCallback = this.OnError;
            this.tService.ReadCallback = this.OnRead;
        }

        private void OnAccept(long id, IPEndPoint ipEndPoint)
        {
            TChannel channel = this.tService.Get(id);
            long timeNow = TimeInfo.Instance.ClientNow();
            this.readWriteTime[id] = timeNow;
            this.channelIds.Enqueue(id);
            this.idEndpoints.Add(id, channel.RemoteAddress);
        }

        public void OnError(long id, int error)
        {
            Log.Warning($"IKcpTransport tcp error: {id} {error}");
            this.tService.Remove(id, error);
            this.idEndpoints.RemoveByKey(id);
            this.readWriteTime.Remove(id);
        }
        
        private void OnRead(long id, MemoryBuffer memoryBuffer)
        {
            long timeNow = TimeInfo.Instance.ClientNow();
            this.readWriteTime[id] = timeNow;
            TChannel channel = this.tService.Get(id);
            channelRecvDatas.Enqueue((channel.RemoteAddress, memoryBuffer));
        }
        
        public void Send(byte[] bytes, int index, int length, EndPoint endPoint, ChannelType channelType)
        {
            long id = this.idEndpoints.GetKeyByValue(endPoint);
            if (id == 0)
            {
                if (channelType != ChannelType.Connect)
                {
                    return;
                }
                
                id = this.tService.NewId();
                this.tService.Create(id, (IPEndPoint)endPoint);
                this.idEndpoints.Add(id, endPoint);
                this.channelIds.Enqueue(id);
            }
            MemoryBuffer memoryBuffer = this.tService.Fetch();
            memoryBuffer.Write(bytes, index, length);
            memoryBuffer.Seek(0, SeekOrigin.Begin);
            this.tService.Send(id, memoryBuffer);
            
            long timeNow = TimeInfo.Instance.ClientNow();
            this.readWriteTime[id] = timeNow;
        }

        public int Recv(byte[] buffer, ref EndPoint endPoint)
        {
            return RecvNonAlloc(buffer, ref endPoint);
        }

        public IPEndPoint GetBindPoint()
        {
            return this.tService.GetBindPoint();
        }

        public int RecvNonAlloc(byte[] buffer, ref EndPoint endPoint)
        {
            (EndPoint e, MemoryBuffer memoryBuffer) = this.channelRecvDatas.Dequeue();
            endPoint = e;
            int count = memoryBuffer.Read(buffer);
            this.tService.Recycle(memoryBuffer);
            return count;
        }

        public bool Available()
        {
            return this.channelRecvDatas.Count > 0;
        }

        public void Update()
        {
            // 检查长时间不读写的TChannel, 超时断开, 一次update检查10个
            long timeNow = TimeInfo.Instance.ClientNow();
            const int MaxCheckNum = 10;
            int n = this.channelIds.Count < MaxCheckNum? this.channelIds.Count : MaxCheckNum;
            for (int i = 0; i < n; ++i)
            {
                long id = this.channelIds.Dequeue();
                if (!this.readWriteTime.TryGetValue(id, out long rwTime))
                {
                    continue;
                }
                if (timeNow - rwTime > 30 * 1000)
                {
                    this.OnError(id, ErrorCore.ERR_KcpReadWriteTimeout);
                    continue;
                }
                this.channelIds.Enqueue(id);
            }
            
            this.tService.Update();
        }

        public void Dispose()
        {
            this.tService?.Dispose();
        }
    }
}

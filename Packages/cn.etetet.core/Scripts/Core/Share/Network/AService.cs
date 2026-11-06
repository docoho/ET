using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace ET
{
    /// <summary>
    /// 底层网络服务（Service）抽象基类，负责管理一组 <see cref="AChannel"/> 通道的创建、移除、轮询收发以及 <see cref="MemoryBuffer"/> 缓冲池。
    /// <remarks>
    /// 常见实现包括：基于 KCP 协议的 <see cref="KService"/> 与基于 TCP 协议的 <see cref="TService"/>。
    /// 上层 <c>NetComponent</c> 通过注册 <see cref="AcceptCallback"/>、<see cref="ReadCallback"/>、<see cref="ErrorCallback"/> 接收底层网络事件。
    /// </remarks>
    /// </summary>
    public abstract class AService: IDisposable
    {
        /// <summary>
        /// 服务端接受到新客户端连接时的回调（参数：channelId, 客户端远端地址）。
        /// </summary>
        public Action<long, IPEndPoint> AcceptCallback;
        /// <summary>
        /// 通道收到完整数据包时的回调（参数：channelId, 消息内存缓冲区）。
        /// </summary>
        public Action<long, MemoryBuffer> ReadCallback;
        /// <summary>
        /// 通道发生错误或断开时的回调（参数：channelId, 错误码）。
        /// </summary>
        public Action<long, int> ErrorCallback;

        /// <summary>
        /// 网络服务实例唯一 Id（置为 0 表示服务已销毁）。
        /// </summary>
        public long Id { get; set; } = IdGenerater.Instance.GenerateId();
        
        /// <summary>
        /// 网络服务类型（外网客户端通信 <see cref="ServiceType.Outer"/> 或服务端内网通信 <see cref="ServiceType.Inner"/>）。
        /// </summary>
        public ServiceType ServiceType { get; protected set; }

        /// <summary>
        /// 获取当前网络服务监听绑定的本地地址（<see cref="IPEndPoint"/>）。
        /// </summary>
        public abstract IPEndPoint GetBindPoint();
        
        /// <summary>
        /// 可回收到缓冲池的 <see cref="MemoryBuffer"/> 默认及最大容量上限（1024 字节）。
        /// </summary>
        private const int MaxMemoryBufferSize = 1024;
		
        /// <summary>
        /// 轻量级 <see cref="MemoryBuffer"/> 对象池，减少高频小包收发时的内存流分配。
        /// </summary>
        private readonly Queue<MemoryBuffer> pool = new();

        /// <summary>
        /// 从缓冲池中获取一个 <see cref="MemoryBuffer"/>（若请求大小超过 1024 字节则直接新建而不走池）。
        /// </summary>
        public MemoryBuffer Fetch(int size = 0)
        {
            if (size > MaxMemoryBufferSize)
            {
                return new MemoryBuffer(size);
            }
            
            if (size < MaxMemoryBufferSize)
            {
                size = MaxMemoryBufferSize;
            }
            
            if (this.pool.Count == 0)
            {
                return new MemoryBuffer(size);
            }
            
            return pool.Dequeue();
        }

        /// <summary>
        /// 将使用完毕的 <see cref="MemoryBuffer"/> 重置并归还到缓冲池（容量超过 1024 或池满 10 个时直接丢弃）。
        /// </summary>
        public void Recycle(MemoryBuffer memoryBuffer)
        {
            if (memoryBuffer.Capacity > 1024)
            {
                return;
            }
            
            if (this.pool.Count > 10) // 这里不需要太大，其实Kcp跟Tcp,这里1就足够了
            {
                return;
            }
            
            memoryBuffer.Seek(0, SeekOrigin.Begin);
            memoryBuffer.SetLength(0);
            
            this.pool.Enqueue(memoryBuffer);
        }
        
        /// <summary>
        /// 释放网络服务。
        /// </summary>
        public virtual void Dispose()
        {
            this.Id = 0;
        }

        /// <summary>
        /// 每帧驱动网络服务轮询收发与通道更新。
        /// </summary>
        public abstract void Update();

        /// <summary>
        /// 移除并关闭指定 Id 的网络通道。
        /// </summary>
        public abstract void Remove(long id, int error = 0);

        /// <summary>
        /// 判断当前网络服务是否已销毁。
        /// </summary>
        public bool IsDisposed()
        {
            return this.Id == 0;
        }
        
        /// <summary>
        /// 根据指定的通道 Id 和远端地址创建一条新的出站连接通道。
        /// </summary>
        public abstract void Create(long id, IPEndPoint ipEndPoint);

        /// <summary>
        /// 通过指定 Id 的通道发送二进制消息缓冲区。
        /// </summary>
        public abstract void Send(long channelId, MemoryBuffer memoryBuffer);

        /// <summary>
        /// 获取指定通道的本地连接号与远端连接号（<c>(localConn, remoteConn)</c>，主要用于 KCP 路由）。
        /// </summary>
        public virtual (uint, uint) GetChannelConn(long channelId)
        {
            throw new Exception($"default conn throw Exception! {channelId}");
        }
        
        /// <summary>
        /// 动态修改指定通道的对端地址（主要用于客户端网络切换或 Router 漫游重连）。
        /// </summary>
        public virtual void ChangeAddress(long channelId, IPEndPoint ipEndPoint)
        {
        }
    }
}
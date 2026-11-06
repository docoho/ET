using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace ET
{
    /// <summary>
    /// <see cref="Session"/> 上发起的外网 RPC 请求挂起等待上下文结构体。
    /// </summary>
    public readonly struct RpcInfo
    {
        /// <summary>
        /// 发送的请求消息类型。
        /// </summary>
        public Type RequestType { get; }
        
        private readonly ETTask<IResponse> tcs;

        public RpcInfo(Type requestType)
        {
            this.RequestType = requestType;
            
            this.tcs = ETTask<IResponse>.Create(true);
        }

        /// <summary>
        /// 收到对端回传的 <see cref="IResponse"/> 时唤醒等待协程。
        /// </summary>
        public void SetResult(IResponse response)
        {
            this.tcs.SetResult(response);
        }

        /// <summary>
        /// 会话异常或断开时向等待协程抛出异常。
        /// </summary>
        public void SetException(Exception exception)
        {
            this.tcs.SetException(exception);
        }

        /// <summary>
        /// 异步等待 RPC 响应返回。
        /// </summary>
        public async ETTask<IResponse> Wait()
        {
            return await this.tcs;
        }
    }

    /// <summary>
    /// 网络会话实体，作为 <see cref="NetComponent"/>（或内网组件）的子实体存在，代表一条高层的网络连接会话。
    /// <remarks>
    /// 它的 <c>Id</c> 与底层 <see cref="AChannel.Id"/> 完全一致；
    /// 封装了向对端发送消息（<c>Send</c>）、发起异步 RPC 调用（<c>Call</c>）、维护挂起请求字典 <see cref="requestCallbacks"/> 以及记录最近收发时间等功能。
    /// 当 <see cref="Session"/> 被 <c>Dispose()</c> 销毁时，会自动关闭底层对应的 <see cref="AChannel"/> 并唤醒所有未完成的 RPC 请求返回错误码。
    /// </remarks>
    /// </summary>
    [ChildOf]
    public sealed class Session: Entity, IAwake<AService>, IDestroy
    {
        /// <summary>
        /// 所属的底层网络服务实例。
        /// </summary>
        public AService AService { get; set; }
        
        /// <summary>
        /// 当前会话自增分配的 RPC 请求流水号。
        /// </summary>
        public int RpcId
        {
            get;
            set;
        }

        /// <summary>
        /// 当前会话上正在等待响应的 RPC 请求回调字典（Key: RpcId, Value: <see cref="RpcInfo"/>）。
        /// </summary>
        public readonly Dictionary<int, RpcInfo> requestCallbacks = new();
        
        /// <summary>
        /// 最近一次收到对端消息的时间戳（用于空闲掉线检测 <see cref="SessionIdleCheckerComponent"/>）。
        /// </summary>
        public long LastRecvTime
        {
            get;
            set;
        }

        /// <summary>
        /// 最近一次向对端发送消息的时间戳（用于空闲掉线检测 <see cref="SessionIdleCheckerComponent"/>）。
        /// </summary>
        public long LastSendTime
        {
            get;
            set;
        }

        /// <summary>
        /// 会话断开或异常时的错误码（0 表示正常关闭，非 0 在销毁时传递给未完成的 RPC 回调）。
        /// </summary>
        public int Error
        {
            get;
            set;
        }

        /// <summary>
        /// 对端网络地址信息。
        /// </summary>
        public Address RemoteAddress
        {
            get;
            set;
        }
    }
}
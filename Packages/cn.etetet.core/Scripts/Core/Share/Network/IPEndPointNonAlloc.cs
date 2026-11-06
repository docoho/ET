using System;
using System.Net;
using System.Net.Sockets;

namespace ET
{
    /// <summary>
    /// 网络端点（<see cref="EndPoint"/>）扩展工具类。
    /// </summary>
    public static class EndPointHelper
    {
        /// <summary>
        /// 深拷贝一个 <see cref="EndPoint"/> 为新的 <see cref="IPEndPoint"/> 实例（避免复用接收地址缓存时被下次 <c>ReceiveFrom</c> 覆盖）。
        /// </summary>
        public static IPEndPoint Clone(this EndPoint endPoint)
        {
            IPEndPoint ip = (IPEndPoint)endPoint;
            ip = new IPEndPoint(ip.Address, ip.Port);
            return ip;
        }
    }
}
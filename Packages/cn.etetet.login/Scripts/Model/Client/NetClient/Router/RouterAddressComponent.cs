using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace ET.Client
{
    /// <summary>
    /// 客户端路由地址列表组件：保存从 RouterManager HTTP 服务拉取到的 Realm 地址列表与 Router 软路由节点列表。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class RouterAddressComponent: Entity, IAwake<string>
    {
        /// <summary>RouterManager 地址对应的 IP 协议族（IPv4 或 IPv6）</summary>
        public AddressFamily AddressFamily { get; set; }
        /// <summary>RouterManager 的 HTTP 服务地址（IP:Port）</summary>
        public string Address;
        /// <summary>可用的 Realm 登录服地址列表</summary>
        public List<string> Realms { get; set; } = new();
        /// <summary>可用的 Router 软路由节点地址列表</summary>
        public List<string> Routers { get; set; } = new();
        /// <summary>当前轮询选取 Router 的索引游标</summary>
        public int RouterIndex;
    }
}
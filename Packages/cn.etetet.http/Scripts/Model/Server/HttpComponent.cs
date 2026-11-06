using System.Collections.Generic;
using System.Net;

namespace ET.Server
{
    /// <summary>
    /// 服务端 HTTP 请求监听与分发组件（挂载在需要提供 HTTP 接口的 <see cref="Scene"/> 上，如 RouterManager、Realm 等）。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class HttpComponent: Entity, IAwake<string>, IDestroy
    {
        /// <summary>底层 .NET HTTP 协议监听器实例。</summary>
        public HttpListener Listener;
    }
}
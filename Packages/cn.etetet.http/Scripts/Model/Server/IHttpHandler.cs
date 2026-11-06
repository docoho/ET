using System.Net;

namespace ET.Server
{
    /// <summary>
    /// 服务端 HTTP 请求处理器统一接口。
    /// <para>实现此接口并标记 <see cref="HttpHandlerAttribute"/> 的无状态类将被 <see cref="HttpDispatcher"/> 自动发现并用于处理指定路径的 HTTP 请求。</para>
    /// </summary>
    public interface IHttpHandler
    {
        /// <summary>
        /// 异步处理单次 HTTP 请求并将响应写入 <paramref name="context"/>。
        /// </summary>
        ETTask Handle(Scene scene, HttpListenerContext context);
    }
}
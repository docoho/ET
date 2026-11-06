namespace ET.Server
{
    /// <summary>
    /// 服务端 HTTP 请求处理器注册特性。
    /// <para>标记在实现 <see cref="IHttpHandler"/> 的类上，用于指定该处理器所属的场景类型（<see cref="SceneType"/>）以及监听的 URL 绝对路径（<see cref="Path"/>）。</para>
    /// </summary>
    public class HttpHandlerAttribute: BaseAttribute
    {
        /// <summary>承载该 HTTP 接口的服务端场景类型（如 <c>SceneType.RouterManager</c>）。</summary>
        public int SceneType { get; }

        /// <summary>HTTP 请求的 URL 路径（如 <c>"/get_router"</c>）。</summary>
        public string Path { get; }

        public HttpHandlerAttribute(int sceneType, string path)
        {
            this.SceneType = sceneType;
            this.Path = path;
        }
    }
}
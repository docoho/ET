using System;
using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 服务端 HTTP 请求路由分发器全局单例。
    /// <para>在启动或热重载时扫描所有标记了 <see cref="HttpHandlerAttribute"/> 的处理器类，构建“请求路径（Path） -> 场景类型（SceneType） -> <see cref="IHttpHandler"/> 实例”的二级路由表。</para>
    /// </summary>
    [CodeProcess]
    [AllowInstance]
    public class HttpDispatcher: Singleton<HttpDispatcher>, ISingletonAwake
    {
        /// <summary>二级路由字典：外层 Key 为 URL 路径（Path），内层 Key 为 SceneType。</summary>
        private readonly Dictionary<string, Dictionary<int, IHttpHandler>> dispatcher = new();
        
        /// <summary>
        /// 扫描并注册所有标记了 <see cref="HttpHandlerAttribute"/> 的 <see cref="IHttpHandler"/> 实现类。
        /// </summary>
        public void Awake()
        {
            HashSet<Type> types = CodeTypes.Instance.GetTypes(typeof (HttpHandlerAttribute));

            foreach (Type type in types)
            {
                object[] attrs = type.GetCustomAttributes(typeof(HttpHandlerAttribute), false);
                if (attrs.Length == 0)
                {
                    continue;
                }

                HttpHandlerAttribute httpHandlerAttribute = (HttpHandlerAttribute)attrs[0];
                
                object obj = Activator.CreateInstance(type);

                IHttpHandler ihttpHandler = obj as IHttpHandler;
                if (ihttpHandler == null)
                {
                    throw new Exception($"HttpHandler handler not inherit IHttpHandler class: {obj.GetType().FullName}");
                }

                if (!this.dispatcher.TryGetValue(httpHandlerAttribute.Path, out var dict))
                {
                    dict = new Dictionary<int, IHttpHandler>();
                    this.dispatcher.Add(httpHandlerAttribute.Path, dict);
                }
                
                dict.Add(httpHandlerAttribute.SceneType, ihttpHandler);
            }
        }

        public IHttpHandler Get(int sceneType, string path)
        {
            return this.dispatcher[path][sceneType];
        }
    }
}

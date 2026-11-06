using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace ET.Server
{
    /// <summary>
    /// 客户端请求获取可用软路由（Router）与登录服（Realm）地址列表的 HTTP 处理器（路径：<c>/get_router</c>）。
    /// <para>从当前场景的 <see cref="ServiceDiscoveryProxy"/> 中读取实时在线的所有 <c>Realm</c> 内网地址与 <c>Router</c> 公网地址，序列化为 JSON 返回给客户端。</para>
    /// </summary>
    [HttpHandler(SceneType.RouterManager, "/get_router")]
    public class HttpGetRouterHandler : IHttpHandler
    {
        public async ETTask Handle(Scene scene, HttpListenerContext context)
        {
            HttpGetRouterResponse httpGetRouterResponse = HttpGetRouterResponse.Create();
            
            ServiceDiscoveryProxy serviceDiscoveryProxy = scene.GetComponent<ServiceDiscoveryProxy>();
            List<ServiceInfo> realmServiceInfos = serviceDiscoveryProxy.GetBySceneType(SceneType.Realm);
            foreach (ServiceInfo serviceInfo in realmServiceInfos)
            {
                // 这里是要用InnerIP，因为云服务器上realm绑定不了OuterIP的,所以realm的内网外网的socket都是监听内网地址
                httpGetRouterResponse.Realms.Add(serviceInfo.Metadata[ServiceMetaKey.InnerIPOuterPort]);
            }
            
            List<ServiceInfo> routerServiceInfos = serviceDiscoveryProxy.GetBySceneType(SceneType.Router);
            foreach (ServiceInfo serviceInfo in routerServiceInfos)
            {
                httpGetRouterResponse.Routers.Add(serviceInfo.Metadata[ServiceMetaKey.OuterIPOuterPort]);
            }
            
            HttpListenerRequest request = context.Request;
            using HttpListenerResponse response = context.Response;
            
            if (request.HttpMethod == "OPTIONS")
            {
                response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
                response.AddHeader("Access-Control-Allow-Methods", "GET, POST");
                response.AddHeader("Access-Control-Max-Age", "1728000");
            }
            response.AppendHeader("Access-Control-Allow-Origin", "*");
            
            byte[] bytes = MongoHelper.ToJson(httpGetRouterResponse).ToUtf8();
            response.StatusCode = 200;
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = bytes.Length;
            EntityRef<Scene> sceneRef = scene;
            await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            scene = sceneRef;
            await scene.Root().TimerComponent.WaitAsync(1000);
        }
    }
}
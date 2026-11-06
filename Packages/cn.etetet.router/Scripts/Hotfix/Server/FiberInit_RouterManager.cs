using System;
using System.Runtime.InteropServices;

namespace ET.Server
{
    /// <summary>
    /// 软路由管理中心场景（<c>SceneType.RouterManager</c>）初始化处理器。
    /// <para>启动 HTTP 监听组件（提供 <c>/get_router</c> 接口），并通过 <see cref="ServiceDiscoveryProxy"/> 订阅全集群所有在线的 <c>Router</c> 和 <c>Realm</c> 节点列表变更。</para>
    /// </summary>
    [Invoke(SceneType.RouterManager)]
    public class FiberInit_RouterManager: AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Scene root = fiberInit.Fiber.Root;
            root.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            root.AddComponent<ProcessInnerSender>();
            root.AddComponent<MessageSender>();

            string httpHost = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "+" : "*";
            int outerPort = AddHttpComponent(root, httpHost);
            root.RemoveComponent<RouterManagerAddressComponent>();
            RouterManagerAddressComponent routerManagerAddressComponent = root.AddComponent<RouterManagerAddressComponent>();
            routerManagerAddressComponent.Address = $"{root.GetSingleton<AddressSingleton>().OuterIP}:{outerPort}";

            ServiceDiscoveryProxy serviceDiscoveryProxy = root.AddComponent<ServiceDiscoveryProxy>();
            EntityRef<ServiceDiscoveryProxy> serviceDiscoveryProxyComponentRef = serviceDiscoveryProxy;

            // 订阅Router
            serviceDiscoveryProxy = serviceDiscoveryProxyComponentRef;
            await serviceDiscoveryProxy.SubscribeServiceChange("Router", 
                new StringKV()
                {
                    {ServiceMetaKey.SceneType, SceneTypeSingleton.Instance.GetSceneName(SceneType.Router)},
                });
            
            // 订阅Realm
            serviceDiscoveryProxy = serviceDiscoveryProxyComponentRef;
            await serviceDiscoveryProxy.SubscribeServiceChange("Realm", 
                new StringKV()
                {
                    {ServiceMetaKey.SceneType, SceneTypeSingleton.Instance.GetSceneName(SceneType.Realm)},
                });
        }

        private static int AddHttpComponent(Scene root, string httpHost)
        {
            bool isTestScene = Options.Instance.SceneName == SceneTypeSingleton.Instance.GetSceneName(SceneType.Test);
            if (!isTestScene)
            {
                int port = AddressHelper.GetSceneOuterPort(root.Fiber(), root.Name.GetSceneConfigName());
                root.AddComponent<HttpComponent, string>($"http://{httpHost}:{port}/");
                return port;
            }

            Exception lastException = null;
            for (int i = 0; i < 10; ++i)
            {
                int port = NetworkHelper.GetFreeTcpPort();
                try
                {
                    root.AddComponent<HttpComponent, string>($"http://{httpHost}:{port}/");
                    return port;
                }
                catch (Exception e)
                {
                    lastException = e;
                    root.RemoveComponent<HttpComponent>();
                    Log.Warning($"router manager dynamic bind failed, port: {port}, attempt: {i + 1}");
                }
            }

            throw new Exception("RouterManager dynamic http bind failed after 10 attempts.", lastException);
        }
    }
}

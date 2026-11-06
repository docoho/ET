using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// MapManager 地图副本管理纤程初始化处理器。
    /// </summary>
    [Invoke(SceneType.MapManager)]
    public class FiberInit_MapManager: AInvokeHandler<FiberInit, ETTask>
    {
        /// <summary>
        /// 挂载邮箱、定时器、协程锁、内网消息收发、Location 代理及 <see cref="MapManagerComponent"/>，并向服务发现注册与订阅 Location 服务。
        /// </summary>
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Scene root = fiberInit.Fiber.Root;
            root.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            root.AddComponent<ProcessInnerSender>();
            root.AddComponent<MessageSender>();
            root.AddComponent<LocationProxyComponent>();
            root.AddComponent<MessageLocationSenderComponent>();
            
            root.AddComponent<MapManagerComponent>();
            
            // 注册服务发现
            ServiceDiscoveryProxy serviceDiscoveryProxy = root.AddComponent<ServiceDiscoveryProxy>();
            EntityRef<ServiceDiscoveryProxy> serviceDiscoveryProxyComponentRef = serviceDiscoveryProxy;
            await serviceDiscoveryProxy.RegisterToServiceDiscovery();
            
            serviceDiscoveryProxy = serviceDiscoveryProxyComponentRef;
            // 订阅location
            await serviceDiscoveryProxy.SubscribeServiceChange("Location", 
                new StringKV()
                {
                    {ServiceMetaKey.SceneType, SceneTypeSingleton.Instance.GetSceneName(SceneType.Location)},
                });
        }
    }
}
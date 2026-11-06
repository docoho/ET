using System;
using System.Collections.Generic;
using System.Net;

namespace ET.Server
{
    /// <summary>
    /// 网关服（SceneType.Gate）纤程初始化回调：挂载基础网络、玩家管理、ActorLocation 组件并向服务发现注册。
    /// </summary>
    [Invoke(SceneType.Gate)]
    public class FiberInit_Gate: AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Scene root = fiberInit.Fiber.Root;
            root.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            root.AddComponent<ProcessInnerSender>();
            root.AddComponent<MessageSender>();
            root.AddComponent<PlayerComponent>();
            root.AddComponent<GateSessionKeyComponent>();
            root.AddComponent<LocationProxyComponent>();
            root.AddComponent<MessageLocationSenderComponent>();

            // 绑定供 Router 转发客户端流量的 UDP 外网端口
            AddressSingleton addressSingleton = root.GetSingleton<AddressSingleton>();
            int outerPort = AddressHelper.GetSceneOuterPort(root.Fiber(), root.Name.GetSceneConfigName());

            IPEndPoint innerIPOuterPort = new Address(addressSingleton.InnerIP, outerPort);
            
            NetComponent netComponent = root.AddComponent<NetComponent, IKcpTransport>(new UdpTransport(innerIPOuterPort));
            
            // 注册服务发现
            ServiceDiscoveryProxy serviceDiscoveryProxy = root.AddComponent<ServiceDiscoveryProxy>();
            EntityRef<ServiceDiscoveryProxy> serviceDiscoveryProxyComponentRef = serviceDiscoveryProxy;
            
            await serviceDiscoveryProxy.RegisterToServiceDiscovery
            (
                new StringKV()
                {
                    { ServiceMetaKey.InnerIPOuterPort, $"{netComponent.GetBindPoint()}" }
                }
            );
            
            // 订阅location
            serviceDiscoveryProxy = serviceDiscoveryProxyComponentRef;
            await serviceDiscoveryProxy.SubscribeServiceChange("Location", 
                new StringKV() { {ServiceMetaKey.SceneType, SceneTypeSingleton.Instance.GetSceneName(SceneType.Location)} });
        }
    }
}

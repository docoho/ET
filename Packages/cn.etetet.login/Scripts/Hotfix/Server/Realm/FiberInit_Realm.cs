using System;
using System.Collections.Generic;
using System.Net;

namespace ET.Server
{
    /// <summary>
    /// 登录服（SceneType.Realm）纤程初始化回调：启动外网监听端口、注册服务发现并订阅同区（Zone）的 Gate 服务列表。
    /// </summary>
    [Invoke(SceneType.Realm)]
    public class FiberInit_Realm: AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Scene root = fiberInit.Fiber.Root;
            root.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            root.AddComponent<ProcessInnerSender>();
            root.AddComponent<MessageSender>();

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
            
            // 订阅跟realm属于同一个zone的Gate
            serviceDiscoveryProxy = serviceDiscoveryProxyComponentRef;
            await serviceDiscoveryProxy.SubscribeServiceChange("Gate", 
                new StringKV()
                {
                    {ServiceMetaKey.SceneType, SceneTypeSingleton.Instance.GetSceneName(SceneType.Gate)},
                    { ServiceMetaKey.Zone, $"{fiberInit.Fiber.Zone}" }
                });
        }
    }
}

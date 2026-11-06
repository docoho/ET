using System;
using System.Collections.Generic;
using System.Net;

namespace ET.Server
{
    /// <summary>
    /// 软路由节点场景（<c>SceneType.Router</c>）初始化处理器。
    /// <para>挂载 <see cref="RouterComponent"/> 监听公网端口并绑定内网转发端口，随后通过 <see cref="ServiceDiscoveryProxy"/> 将自身的公网接入地址注册到服务发现中心，供 <c>RouterManager</c> 下发给客户端。</para>
    /// </summary>
    [Invoke(SceneType.Router)]
    public class FiberInit_Router: AInvokeHandler<FiberInit, ETTask>
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
            string innerIP = addressSingleton.InnerIP;
            IPEndPoint outerIPOutPort = new Address(addressSingleton.OuterIP, outerPort);
            
            // 开发期间使用OuterIPPort，云服务器因为本机没有OuterIP，所以要改成InnerIPPort，然后在云防火墙中端口映射到InnerIPPort
            RouterComponent routerComponent = root.AddComponent<RouterComponent, IPEndPoint, string>(outerIPOutPort, innerIP);
            
            // 注册服务发现
            ServiceDiscoveryProxy serviceDiscoveryProxy = root.AddComponent<ServiceDiscoveryProxy>();
            await serviceDiscoveryProxy.RegisterToServiceDiscovery(new StringKV()
            {
                { ServiceMetaKey.OuterIPOuterPort, $"{routerComponent.GetBindPoint()}" }
            });
        }
    }
}

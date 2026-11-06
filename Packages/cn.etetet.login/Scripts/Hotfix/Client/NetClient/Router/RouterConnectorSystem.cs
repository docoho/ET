using System;
using System.Net;

namespace ET.Client
{
    /// <summary>
    /// 客户端路由连接握手组件系统：向 KService 注册 RouterACK 回调并发送底层路由握手包。
    /// </summary>
    [EntitySystemOf(typeof(RouterConnector))]
    public static partial class RouterConnectorSystem
    {
        [EntitySystem]
        private static void Awake(this RouterConnector self)
        {
            NetComponent netComponent = self.GetParent<NetComponent>();
            KService kService = (KService)netComponent.AService;
            EntityRef<RouterConnector> routerConnectorRef = self;
            kService.AddRouterAckCallback(self.Id, (flag) =>
            {
                RouterConnector routerConnector = routerConnectorRef;
                if (routerConnector == null)
                {
                    return;
                }
                routerConnector.Flag = flag;
            });
        }
        [EntitySystem]
        private static void Destroy(this RouterConnector self)
        {
            NetComponent netComponent = self.GetParent<NetComponent>();
            KService kService = (KService)netComponent.AService;
            kService.RemoveRouterAckCallback(self.Id);
        }

        /// <summary>
        /// 通过底层 KService Transport 向目标 Router 节点发送握手数据包。
        /// </summary>
        public static void Connect(this RouterConnector self, byte[] bytes, int index, int length, IPEndPoint ipEndPoint)
        {
            NetComponent netComponent = self.GetParent<NetComponent>();
            KService kService = (KService)netComponent.AService;
            kService.Transport.Send(bytes, index, length, ipEndPoint, ChannelType.Connect);
        }
    }
}
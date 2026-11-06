using System;
using System.Net;

namespace ET.Client
{
    /// <summary>
    /// 客户端路由连接辅助工具类：封装通过 Router 软路由节点建立与后端服（Realm/Gate）KCP 会话及重连握手的完整流程。
    /// </summary>
    public static partial class RouterHelper
    {
        /// <summary>
        /// 创建经过 Router 代理转发的 Session 会话，并自动挂载 PingComponent 和 RouterCheckComponent。
        /// </summary>
        // 注册router
        public static async ETTask<Session> CreateRouterSession(this NetComponent netComponent, IPEndPoint address, string account, string password)
        {
            EntityRef<NetComponent> netComponentRef = netComponent;
            uint localConn = (uint)(account.GetLongHashCode() ^ password.GetLongHashCode() ^ RandomGenerator.RandUInt32());
            (uint recvLocalConn, IPEndPoint routerAddress) = await GetRouterAddress(netComponent, address, localConn, 0);
            
            netComponent = netComponentRef;
            if (recvLocalConn == 0)
            {
                throw new Exception($"get router fail: {netComponent.Root().Id} {address}");
            }
            
            Log.Info($"get router: {recvLocalConn} {routerAddress}");

            Session routerSession = netComponent.Create(routerAddress, address, recvLocalConn);
            routerSession.AddComponent<PingComponent>();
            routerSession.AddComponent<RouterCheckComponent>();
            
            return routerSession;
        }
        
        /// <summary>
        /// 从 RouterAddressComponent 选取一个 Router 节点并发起连接握手。
        /// </summary>
        public static async ETTask<(uint, IPEndPoint)> GetRouterAddress(this NetComponent netComponent, IPEndPoint address, uint localConn, uint remoteConn)
        {
            EntityRef<NetComponent> netComponentRef = netComponent;
            Log.Info($"start get router address: {netComponent.Root().Id} {address} {localConn} {remoteConn}");
            //return (RandomHelper.RandUInt32(), address);
            RouterAddressComponent routerAddressComponent = netComponent.Root().GetComponent<RouterAddressComponent>();
            IPEndPoint routerInfo = routerAddressComponent.GetAddress();
            
            uint recvLocalConn = await netComponent.Connect(routerInfo, address, localConn, remoteConn);
            netComponent = netComponentRef;
            Log.Info($"finish get router address: {netComponent.Root().Id} {address} {localConn} {remoteConn} {recvLocalConn} {routerInfo}");
            return (recvLocalConn, routerInfo);
        }

        // 向router申请
        private static async ETTask<uint> Connect(this NetComponent netComponent, IPEndPoint routerAddress, IPEndPoint realAddress, uint localConn, uint remoteConn)
        {
            uint synFlag = remoteConn == 0? KcpProtocalType.RouterSYN : KcpProtocalType.RouterReconnectSYN;

            // 注意，session也以localConn作为id，所以这里不能用localConn作为id
            long id = (long)(((ulong)localConn << 32) | remoteConn);
            using EntityRef<RouterConnector> routerConnectorRef = netComponent.AddChildWithId<RouterConnector>(id);
            RouterConnector routerConnector = routerConnectorRef;
            int count = 20;
            byte[] sendCache = new byte[512];

            uint connectId = RandomGenerator.RandUInt32();
            sendCache.WriteTo(0, synFlag);
            sendCache.WriteTo(1, localConn);
            sendCache.WriteTo(5, remoteConn);
            sendCache.WriteTo(9, connectId);
            byte[] addressBytes = realAddress.ToString().ToByteArray();
            Array.Copy(addressBytes, 0, sendCache, 13, addressBytes.Length);
            TimerComponent timerComponent = netComponent.Root().TimerComponent;
            EntityRef<TimerComponent> timerComponentRef = timerComponent;
            Log.Info($"router connect: {localConn} {remoteConn} {routerAddress} {realAddress}");

            long lastSendTimer = 0;

            while (true)
            {
                timerComponent = timerComponentRef;
                if (timerComponent == null)
                {
                    return 0;
                }

                long timeNow = timerComponent.GetSingleton<TimeInfo>().ClientNow();
                if (timeNow - lastSendTimer > 300)
                {
                    if (--count < 0)
                    {
                        Log.Error($"router connect timeout fail! {localConn} {remoteConn} {routerAddress} {realAddress}");
                        return 0;
                    }

                    lastSendTimer = timeNow;

                    routerConnector = routerConnectorRef;
                    // 发送
                    routerConnector.Connect(sendCache, 0, addressBytes.Length + 13, routerAddress);
                }

                await timerComponent.WaitFrameAsync();

                routerConnector = routerConnectorRef;
                if (routerConnector.Flag == 0)
                {
                    continue;
                }

                return localConn;
            }
        }
    }
}

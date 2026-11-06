using System;
using System.Net;

namespace ET.Client
{
    /// <summary>
    /// 客户端路由健康检测与无感切换组件系统：每秒检测一次 Session 最后收包时间，若超过 7 秒未收到包则自动切换新的 Router 节点进行重连。
    /// </summary>
    [EntitySystemOf(typeof(RouterCheckComponent))]
    public static partial class RouterCheckComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RouterCheckComponent self)
        {
            self.CheckAsync().Coroutine();
        }

        /// <summary>
        /// 周期性检测接收超时并向新 Router 节点发起 RouterReconnectSYN 重连握手，成功后热更新底层 KChannel 的目标路由地址。
        /// </summary>
        private static async ETTask CheckAsync(this RouterCheckComponent self)
        {
            Session session = self.GetParent<Session>();
            Fiber fiber = self.Fiber();
            Scene root = fiber.Root;
            
            Address realAddress = session.RemoteAddress;
            NetComponent netComponent = root.GetComponent<NetComponent>();
            
            EntityRef<RouterCheckComponent> selfRef = self;
            EntityRef<Session> sessionRef = session;
            EntityRef<Scene> rootRef = root;
            EntityRef<NetComponent> netComponentRef = netComponent;
            while (true)
            {
                await fiber.Root.TimerComponent.WaitAsync(1000);
                
                self = selfRef;
                session = sessionRef;
                
                if (self == null)
                {
                    return;
                }
                if (session == null)
                {
                    return;
                }

                long time = self.GetSingleton<TimeInfo>().ClientNow();

                if (time - session.LastRecvTime < 7 * 1000)
                {
                    continue;
                }
                
                try
                {
                    long sessionId = session.Id;

                    (uint localConn, uint remoteConn) = session.AService.GetChannelConn(sessionId);
                    
                    root = rootRef;
                    netComponent = netComponentRef;
                    
                    Log.Info($"get recvLocalConn start: {root.Id} {realAddress} {localConn} {remoteConn}");

                    (uint recvLocalConn, IPEndPoint routerAddress) = await netComponent.GetRouterAddress(realAddress, localConn, remoteConn);
                    session = sessionRef;
                    root = rootRef;
                    if (recvLocalConn == 0)
                    {
                        Log.Error($"get recvLocalConn fail: {root.Id} {routerAddress} {realAddress} {localConn} {remoteConn}");
                        continue;
                    }
                    
                    Log.Info($"get recvLocalConn ok: {root.Id} {routerAddress} {realAddress} {recvLocalConn} {localConn} {remoteConn}");
                    
                    session.LastRecvTime = session.GetSingleton<TimeInfo>().ClientNow();
                    
                    session.AService.ChangeAddress(sessionId, routerAddress);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }
    }
}

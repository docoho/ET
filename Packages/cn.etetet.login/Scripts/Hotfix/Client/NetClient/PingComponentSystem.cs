using System;

namespace ET.Client
{
    /// <summary>
    /// 客户端心跳与时间同步组件系统：每 2 秒向网关发送一次 C2G_Ping，计算网络延迟（Ping）并校准服务器与客户端的时间差（ServerMinusClientTime）。
    /// </summary>
    [EntitySystemOf(typeof(PingComponent))]
    public static partial class PingComponentSystem
    {
        [EntitySystem]
        private static void Awake(this PingComponent self)
        {
            self.PingAsync().Coroutine();
        }
        
        [EntitySystem]
        private static void Destroy(this PingComponent self)
        {
            self.Ping = 0;
        }
        
        /// <summary>
        /// 周期性发送心跳请求并更新 RTT 与服务器时间偏移量。
        /// </summary>
        private static async ETTask PingAsync(this PingComponent self)
        {
            Session session = self.GetParent<Session>();
            EntityRef<Session> sessionRef = session;
            EntityRef<PingComponent> selfRef = self;
            Fiber fiber = self.Fiber();
            
            while (true)
            {
                try
                {
                    await fiber.Root.TimerComponent.WaitAsync(2000);
                    self = selfRef;
                    if (self == null)
                    {
                        return;
                    }
                    session = sessionRef;
                    if (session == null)
                    {
                        return;
                    }
                    long time1 = self.GetSingleton<TimeInfo>().ClientNow();
                    // C2G_Ping不需要调用dispose，Call中会判断，如果用了对象池会自动回收
                    C2G_Ping c2GPing = C2G_Ping.Create(true);
                    // 这里response要用using才能回收到池，默认不回收
                    using G2C_Ping response = await session.Call(c2GPing) as G2C_Ping;
                    self = selfRef;
                    if (self == null)
                    {
                        return;
                    }

                    long time2 = self.GetSingleton<TimeInfo>().ClientNow();
                    self.Ping = time2 - time1;
                    
                    self.GetSingleton<TimeInfo>().ServerMinusClientTime = response.Time + (time2 - time1) / 2 - time2;
                }
                catch (RpcException e)
                {
                    // session断开导致ping rpc报错，记录一下即可，不需要打成error
                    Log.Debug($"session disconnect, ping error: {self.Id} {e.Error}");
                    return;
                }
                catch (Exception e)
                {
                    Log.Debug($"ping error: \n{e}");
                }
            }
        }
    }
}

namespace ET.Server
{
    /// <summary>
    /// 单个客户端软路由会话节点（<see cref="RouterNode"/>）的系统类。
    /// <para>管理单个外网客户端连接的生命周期状态，并提供每秒外网收包速率限流检查（<see cref="CheckOuterCount"/>），防止单连接洪水攻击。</para>
    /// </summary>
    [EntitySystemOf(typeof(RouterNode))]
    public static partial class RouterNodeSystem
    {
        [EntitySystem]
        private static void Awake(this RouterNode self)
        {
            long timeNow = self.GetSingleton<TimeInfo>().ServerNow();
            self.LastRecvInnerTime = timeNow;
            self.LastRecvOuterTime = timeNow;
            self.OuterIpEndPoint = null;
            self.InnerIpEndPoint = null;
            self.RouterSyncCount = 0;
            self.InnerConn = 0;
        }

        [EntitySystem]
        private static void Destroy(this RouterNode self)
        {
            self.InnerConn = 0;
            self.LastRecvInnerTime = 0;
            self.LastRecvOuterTime = 0;
            self.OuterIpEndPoint = null;
            self.InnerIpEndPoint = null;
            self.InnerAddress = null;
            self.RouterSyncCount = 0;
            self.SyncCount = 0;
        }
        
        /// <summary>
        /// 统计并检查该路由节点来自外网客户端的每秒收包数量；若每秒超过 1000 个包则返回 <c>false</c> 触发限流断开。
        /// </summary>
        public static bool CheckOuterCount(this RouterNode self, long timeNow)
        {
            if (self.LastCheckTime == 0)
            {
                self.LastCheckTime = timeNow;
            }
            
            if (timeNow - self.LastCheckTime > 1000)
            {
                //Log.Debug($"router recv packet per second: {self.LimitCountPerSecond}");
                self.LimitCountPerSecond = 0;
                self.LastCheckTime = timeNow;
            }

            if (++self.LimitCountPerSecond > 1000)
            {
                return false;
            }

            return true;
        }
    }
}
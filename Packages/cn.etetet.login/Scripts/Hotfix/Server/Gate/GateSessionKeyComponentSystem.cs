namespace ET.Server
{
    /// <summary>
    /// 网关登录凭据（SessionKey）管理组件系统：负责缓存 Realm 申请的临时登录 Key，并在 20 秒超时后自动清理。
    /// </summary>
    public static partial class GateSessionKeyComponentSystem
    {
        /// <summary>
        /// 添加登录 Key 与账号的映射，并启动 20 秒超时自动清理协程。
        /// </summary>
        public static void Add(this GateSessionKeyComponent self, long key, string account)
        {
            self.sessionKey.Add(key, account);
            self.TimeoutRemoveKey(key).Coroutine();
        }

        /// <summary>
        /// 根据登录 Key 查询对应的账号名称（若不存在或已过期则返回 null）。
        /// </summary>
        public static string Get(this GateSessionKeyComponent self, long key)
        {
            string account = null;
            self.sessionKey.TryGetValue(key, out account);
            return account;
        }

        /// <summary>
        /// 主动移除指定的登录 Key。
        /// </summary>
        public static void Remove(this GateSessionKeyComponent self, long key)
        {
            self.sessionKey.Remove(key);
        }

        /// <summary>
        /// 等待 20 秒后自动移除未使用的登录 Key，防止内存泄漏与重放。
        /// </summary>
        private static async ETTask TimeoutRemoveKey(this GateSessionKeyComponent self, long key)
        {
            EntityRef<GateSessionKeyComponent> selfRef = self;
            await self.Root().TimerComponent.WaitAsync(20000);
            self = selfRef;
            self.sessionKey.Remove(key);
        }
    }
}
namespace ET.Server
{
    /// <summary>
    /// 服务端 Actor 辅助扩展类。
    /// </summary>
    public static class ActorHelper
    {
        /// <summary>
        /// 获取指定实体的完整分布式通信地址（<see cref="ActorId"/>），
        /// 包含当前进程的内网监听地址（<see cref="AddressSingleton.InnerAddress"/>）以及该实体所在的纤程 ID 与实例 ID（<see cref="FiberInstanceId"/>）。
        /// </summary>
        /// <param name="self">目标实体。</param>
        /// <returns>可用于跨进程/跨纤程寻址的 <see cref="ActorId"/>。</returns>
        public static ActorId GetActorId(this Entity self)
        {
            Fiber root = self.Fiber();
            return new ActorId(self.GetSingleton<AddressSingleton>().InnerAddress, new FiberInstanceId(root.Id, self.InstanceId));
        }
    }
}

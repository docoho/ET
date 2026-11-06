namespace ET
{
    /// <summary>
    /// <see cref="Unit"/> 单位实体系统，负责初始化配置 ID 并提供关联配置表查询。
    /// </summary>
    [EntitySystemOf(typeof(Unit))]
    public static partial class UnitSystem
    {
        /// <summary>
        /// 初始化 Unit 实例并记录其配置表 ID。
        /// </summary>
        [EntitySystem]
        private static void Awake(this Unit self, int configId)
        {
            self.ConfigId = configId;
        }

        /// <summary>
        /// 获取当前 Unit 对应的 <see cref="UnitConfig"/> 配置表数据。
        /// </summary>
        public static UnitConfig Config(this Unit self)
        {
            return self.Fiber().GetSingleton<UnitConfigCategory>().Get(self.ConfigId);
        }
    }
}

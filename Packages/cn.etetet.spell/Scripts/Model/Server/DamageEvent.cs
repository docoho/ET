namespace ET.Server
{
    /// <summary>
    /// 单位死亡事件结构体，当伤害结算导致目标 HP 归零时发布。
    /// </summary>
    public struct UnitDie
    {
        /// <summary>
        /// 击杀者（施法者）Unit 引用。
        /// </summary>
        public EntityRef<Unit> Unit;
        /// <summary>
        /// 死亡的目标 Unit 引用。
        /// </summary>
        public EntityRef<Unit> Target;
    }
}
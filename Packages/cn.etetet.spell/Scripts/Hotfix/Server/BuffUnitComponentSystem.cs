namespace ET.Server
{
    /// <summary>
    /// Buff 关联召唤物单位记录组件系统（当 Buff 销毁时自动清理移除其创建的所有召唤物 <see cref="Unit"/>）。
    /// </summary>
    [EntitySystemOf(typeof(BuffUnitComponent))]
    public static partial class BuffUnitComponentSystem
    {
        /// <summary>
        /// 组件唤醒初始化。
        /// </summary>
        [EntitySystem]
        private static void Awake(this BuffUnitComponent self)
        {

        }

        /// <summary>
        /// 组件销毁时，从场景的 <see cref="UnitComponent"/> 中移除所有关联的召唤物单位。
        /// </summary>
        [EntitySystem]
        private static void Destroy(this BuffUnitComponent self)
        {
            UnitComponent unitComponent = self.Scene().GetComponent<UnitComponent>();
            if (unitComponent.IsDisposed)
            {
                return;
            }
            
            foreach (long unitId in self.UnitIds)
            {
                unitComponent.Remove(unitId);
            }
        }
    }
}
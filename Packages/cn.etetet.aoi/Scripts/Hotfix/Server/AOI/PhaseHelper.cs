namespace ET.Server
{
    /// <summary>
    /// 单位 AOI 相位（Phase）操作辅助扩展类（用于实现同地图不同位面的视野隔离或共享）。
    /// </summary>
    public static class PhaseHelper
    {
        /// <summary>
        /// 判断单位当前是否包含指定的相位标记 <paramref name="phaseType"/>。
        /// </summary>
        public static bool IsPhase(this Unit self, PhaseType phaseType)
        {
            AOIEntity aoiEntity = self.GetComponent<AOIEntity>();
            return (aoiEntity.Phase & phaseType) == phaseType;
        }

        /// <summary>
        /// 直接设置单位的相位为 <paramref name="phaseType"/> 并同步写入 <see cref="NumericType.Phase"/>。
        /// </summary>
        public static void SetPhase(this Unit self, PhaseType phaseType)
        {
            AOIEntity aoiEntity = self.GetComponent<AOIEntity>();
            aoiEntity.Phase = phaseType;
            self.NumericComponent?.Set(NumericType.Phase, (long)aoiEntity.Phase);
        }

        /// <summary>
        /// 为单位叠加指定的相位标记 <paramref name="phaseType"/> 并同步写入 <see cref="NumericType.Phase"/>。
        /// </summary>
        public static void AddPhase(this Unit self, PhaseType phaseType)
        {
            AOIEntity aoiEntity = self.GetComponent<AOIEntity>();
            aoiEntity.Phase |= phaseType;
            self.NumericComponent?.Set(NumericType.Phase, (long)aoiEntity.Phase);
        }

        /// <summary>
        /// 移除单位身上的指定相位标记 <paramref name="phaseType"/> 并同步写入 <see cref="NumericType.Phase"/>。
        /// </summary>
        public static void RemovePhase(this Unit self, PhaseType phaseType)
        {
            AOIEntity aoiEntity = self.GetComponent<AOIEntity>();
            aoiEntity.Phase &= ~phaseType;
            self.NumericComponent?.Set(NumericType.Phase, (long)aoiEntity.Phase);
        }
    }
}
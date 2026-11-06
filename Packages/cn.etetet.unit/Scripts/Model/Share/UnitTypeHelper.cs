namespace ET
{
    /// <summary>
    /// <see cref="UnitType"/> 单位类型位掩码辅助类。
    /// </summary>
    public static class UnitTypeHelper
    {
        /// <summary>
        /// 判断两个 <see cref="UnitType"/> 枚举值是否存在交集（按位与不为 0）。
        /// </summary>
        public static bool IsSame(this UnitType a, UnitType b)
        {
            return (a & b) != 0;
        }
    }
}
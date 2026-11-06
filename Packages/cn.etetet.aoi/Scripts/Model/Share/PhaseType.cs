using System;

namespace ET
{
    /// <summary>
    /// AOI 位面/相位枚举（支持 Flags 按位组合，只有相位按位与不为 0 的两个单位才能互相看见）。
    /// </summary>
    [Flags]
    public enum PhaseType: long
    {
        /// <summary>
        /// 默认普通位面。
        /// </summary>
        Normal = 1, // 普通阶段

        /// <summary>
        /// 位面 1。
        /// </summary>
        Phase1 = 2,

        /// <summary>
        /// 位面 2。
        /// </summary>
        Phase2 = 4,
    }
}
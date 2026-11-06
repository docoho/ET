namespace ET
{
    /// <summary>
    /// 定时器回调类型扩展定义（Map 模块）。
    /// </summary>
    public static partial class TimerInvokeType
    {
        /// <summary>
        /// Buff 超时定时器回调类型。
        /// </summary>
        public const int BuffTimeoutTimer = PackageType.Map * 1000 + 1;

        /// <summary>
        /// 地图分线合并检查定时器回调类型。
        /// </summary>
        public const int MergeLinesCheckTimer = PackageType.Map * 1000 + 2;
    }
}
namespace ET
{
    public static partial class TimerInvokeType
    {
        /// <summary>
        /// Buff 周期性触发（Tick）定时器回调类型。
        /// </summary>
        public const int BuffTickTimer = PackageType.Spell * 1000 + 1;
        /// <summary>
        /// AI 定时器回调类型。
        /// </summary>
        public const int AITimer = PackageType.Spell * 1000 + 2;
    }
}

namespace ET
{
    /// <summary>
    /// 定时器回调事件类型（TimerInvokeType）常量分部类定义（配合 <c>[Invoke(TimerInvokeType.XXX)]</c> 与 <see cref="ATimer{T}"/> 使用）。
    /// </summary>
    [UniqueId]
    public static partial class TimerInvokeType
    {
        /// <summary>
        /// 服务端接受到新 Session 后，若在规定时间内未完成认证/首包交互则自动断开的超时定时器。
        /// </summary>
        public const int SessionAcceptTimeout = PackageType.Core * 1000 + 1;
        /// <summary>
        /// 定期检查 Session 是否长时间无收发数据（空闲掉线检测）的周期定时器。
        /// </summary>
        public const int SessionIdleChecker = PackageType.Core * 1000 + 2;
        
        // Test
        /// <summary>
        /// 单元测试用单次定时器。
        /// </summary>
        public const int TestOnceTimer = PackageType.Core * 1000 + 100;
        /// <summary>
        /// 单元测试用重复周期定时器。
        /// </summary>
        public const int TestRepeatedTimer = PackageType.Core * 1000 + 101;
    }
}
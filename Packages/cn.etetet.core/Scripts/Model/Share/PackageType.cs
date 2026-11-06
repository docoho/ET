namespace ET
{
    /// <summary>
    /// 模块包唯一整型标识常量分部类（标记 <see cref="UniqueIdAttribute"/> 由编译期分析器保证无重复值）。
    /// <remarks>
    /// 各个包在各自的分部类中定义自己的包编号，并以此编号乘以 1000 作为该包内 <c>ErrorCode</c>、<c>CoroutineLockType</c>、<c>TimerInvokeType</c>、<c>MailBoxType</c> 的号段前缀。
    /// </remarks>
    /// </summary>
    [UniqueId]
    public static partial class PackageType
    {
        /// <summary>
        /// 核心包（<c>cn.etetet.core</c>）的唯一编号（1）。
        /// </summary>
        public const int Core = 1;
    }
}
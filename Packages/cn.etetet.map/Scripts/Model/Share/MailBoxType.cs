namespace ET
{
    /// <summary>
    /// 邮箱类型扩展定义（Map 模块）。
    /// </summary>
    public static partial class MailBoxType
    {
        /// <summary>
        /// 客户端根场景邮箱类型（支持将 <see cref="ICurrentMessage"/> 转发至当前活动子场景）。
        /// </summary>
        public const int ClientMessage = PackageType.Map * 1000 + 2;
    }
}

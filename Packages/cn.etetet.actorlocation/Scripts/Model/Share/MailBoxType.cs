namespace ET
{
    /// <summary>
    /// 邮箱类型扩展定义（ActorLocation 模块）。
    /// </summary>
    public static partial class MailBoxType
    {
        /// <summary>
        /// 严格顺序处理的邮箱类型（通过协程锁保证发往同一实体的消息按到达顺序依次处理）。
        /// </summary>
        public const int OrderedMessage = PackageType.ActorLocation * 1000 + 1;
    }
}
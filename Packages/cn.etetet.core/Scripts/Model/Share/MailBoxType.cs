namespace ET
{
    /// <summary>
    /// Actor 邮箱（<see cref="MailBoxComponent"/>）消息处理策略类型常量分部类定义。
    /// </summary>
    public static partial class MailBoxType
    {
        /// <summary>
        /// 无序并发消息邮箱：收到的每条消息直接启动独立协程并发处理，不加协程锁排队（性能最高，适合无需跨 await 严格保序的实体，如 Scene 级服务）。
        /// </summary>
        public const int UnOrderedMessage = PackageType.Core * 1000 + 1;
    }
}
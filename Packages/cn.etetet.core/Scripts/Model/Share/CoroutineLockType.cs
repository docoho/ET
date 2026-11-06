namespace ET
{
    /// <summary>
    /// 协程锁（<see cref="CoroutineLockComponent"/>）类型常量分部类定义。
    /// <remarks>
    /// 每个包使用 <c>PackageType.XXX * 1000 + 序号</c> 的分段规则定义该包专属的协程锁类型，防止多包之间 ID 冲突。
    /// </remarks>
    /// </summary>
    public static partial class CoroutineLockType
    {
        /// <summary>
        /// Actor 邮箱（<see cref="MailBoxComponent"/>）有序消息队列处理使用的协程锁。
        /// </summary>
        public const int Mailbox = PackageType.Core * 1000 + 1;                   // Mailbox中队列
        /// <summary>
        /// 单元测试专用的协程锁类型。
        /// </summary>
        public const int TestLock = PackageType.Core * 1000 + 100;                // 测试用锁
    }
}
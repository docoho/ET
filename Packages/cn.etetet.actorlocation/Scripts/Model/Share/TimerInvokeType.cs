namespace ET
{
    /// <summary>
    /// 定时器回调类型扩展定义（ActorLocation 模块）。
    /// </summary>
    public static partial class TimerInvokeType
    {
        /// <summary>
        /// 定期检查并清理空闲超时 <see cref="Server.MessageLocationSender"/> 的定时器回调类型。
        /// </summary>
        public const int MessageLocationSenderChecker = PackageType.ActorLocation * 1000 + 2;
    }
}
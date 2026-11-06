namespace ET
{
    /// <summary>
    /// <see cref="Kcp"/> 类的扩展常量定义分部类。
    /// </summary>
    public partial class Kcp
    {
        /// <summary>
        /// 1 MB 字节数常量（1024 * 1024）。
        /// </summary>
        public const int OneM = 1024 * 1024;
        /// <summary>
        /// 内网 KCP 通道允许积压的最大未确认待发送包数上限（超过则触发 <see cref="ErrorCore.ERR_KcpWaitSendSizeTooLarge"/> 断开连接）。
        /// </summary>
        public const int InnerMaxWaitSize = 1024 * 1024;
        /// <summary>
        /// 外网 KCP 通道允许积压的最大未确认待发送包数上限。
        /// </summary>
        public const int OuterMaxWaitSize = 1024 * 1024;
    }
    
    
}
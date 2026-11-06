namespace ET
{
    /// <summary>
    /// 邮箱类型常量扩展。
    /// </summary>
    public static partial class MailBoxType
    {
        /// <summary>网关客户端会话转发邮箱：收到内网消息后直接通过客户端外网 Session 发送给客户端</summary>
        public const int GateSession = PackageType.Login * 1000 + 1;
    }
}
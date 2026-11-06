namespace ET
{
    /// <summary>
    /// 错误码常量扩展（Router 软路由模块）。
    /// </summary>
    public static partial class ErrorCode
    {
        /// <summary>KCP 软路由握手连接超时失败。</summary>
        public const int ERR_KcpRouterConnectFail = ErrorCode.ERR_WithException + PackageType.Router * 1000 + 1; // 100015001
        /// <summary>KCP 软路由正常转发期间客户端长时间无报文超时。</summary>
        public const int ERR_KcpRouterTimeout = ErrorCode.ERR_WithException + PackageType.Router * 1000 + 2; // 100015002
        /// <summary>KCP 软路由重连时检测到不同的 ConnectId，清理旧路由节点。</summary>
        public const int ERR_KcpRouterSame = ErrorCode.ERR_WithException + PackageType.Router * 1000 + 3; // 100015003
        /// <summary>KCP 软路由 RouterSync 握手重试次数过多。</summary>
        public const int ERR_KcpRouterRouterSyncCountTooMuchTimes = ErrorCode.ERR_WithException + PackageType.Router * 1000 + 4; // 100015004
        /// <summary>KCP 软路由单个外网连接每秒发包数超过阈值被限流断开。</summary>
        public const int ERR_KcpRouterTooManyPackets = ErrorCode.ERR_WithException + PackageType.Router * 1000 + 5; // 100015005
        /// <summary>KCP 软路由 SYN 握手重试次数过多。</summary>
        public const int ERR_KcpRouterSyncCountTooMuchTimes = ErrorCode.ERR_WithException + PackageType.Router * 1000 + 6; // 100015006
    }
}
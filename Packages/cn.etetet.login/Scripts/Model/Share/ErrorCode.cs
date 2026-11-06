namespace ET
{
    /// <summary>
    /// 登录模块错误码常量扩展。
    /// </summary>
    public static partial class ErrorCode
    {
        /// <summary>客户端连接 Gate 时提供的登录凭据 Key 无效或已过期</summary>
        public const int ERR_ConnectGateKeyError = ErrorCode.ERR_WithException + PackageType.Login * 1000 + 1; // 100009001
    }
}
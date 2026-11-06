namespace ET
{
    /// <summary>
    /// 业务层与框架层共享的错误码（ErrorCode）分部类定义。
    /// <remarks>
    /// 错误码设计规范：
    /// 1. 0（<see cref="ErrorCore.ERR_Success"/>）代表成功；
    /// 2. 带有 <see cref="ERR_WithException"/>（100000000）偏移量的错误码表示系统级异常，RPC 调用收到此类错误码时会自动抛出 <see cref="RpcException"/>；
    /// 3. 不带 <see cref="ERR_WithException"/> 的错误码表示常规业务错误（如金币不足、背包已满），RPC 调用直接返回带该 Error 的 Response，由业务代码通过 <c>if (response.Error != ErrorCore.ERR_Success)</c> 判断处理。
    /// </remarks>
    /// </summary>
    public static partial class ErrorCode
    {
        /// <summary>
        /// 目标 Actor 实体不存在或已销毁（通常触发 Location 重定位或清理缓存）。
        /// </summary>
        public const int ERR_NotFoundActor = ErrorCode.ERR_WithException + PackageType.Core * 1000 + 1; // 100001001
        /// <summary>
        /// RPC 远程调用执行失败（对端处理抛出未捕获异常或通道断开）。
        /// </summary>
        public const int ERR_RpcFail = ErrorCode.ERR_WithException + PackageType.Core * 1000 + 2; // 100001002
        /// <summary>
        /// Actor 消息等待响应超时。
        /// </summary>
        public const int ERR_MessageTimeout = ErrorCode.ERR_WithException + PackageType.Core * 1000 + 3; // 100001003
        /// <summary>
        /// Session 会话长时间无收发数据空闲超时。
        /// </summary>
        public const int ERR_SessionSendOrRecvTimeout = ErrorCode.ERR_WithException + PackageType.Core * 1000 + 4; // 100001004
        /// <summary>
        /// 单位时间内发送消息频率过高（触发防刷包限流断开）。
        /// </summary>
        public const int ERR_MessageCountTooMany = ErrorCode.ERR_WithException + PackageType.Core * 1000 + 5; // 100001005
        
        /// <summary>
        /// 异常错误码偏移量基准（大于此值的错误码会在 RPC 返回时自动抛出 <see cref="RpcException"/>）。
        /// </summary>
        public const int ERR_WithException = 100000000;
    }
}
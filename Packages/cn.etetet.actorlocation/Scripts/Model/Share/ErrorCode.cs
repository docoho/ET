namespace ET
{
    /// <summary>
    /// ActorLocation 模块错误码定义。
    /// </summary>
    public static partial class ErrorCode
    {
        /// <summary>
        /// 位置记录已存在。
        /// </summary>
        public const int ERR_LocationAlreadyExists = ErrorCode.ERR_WithoutException + PackageType.ActorLocation * 1000 + 7; // 200003007

        /// <summary>
        /// 位置已被锁定，无法重复加锁或直接修改。
        /// </summary>
        public const int ERR_LocationAlreadyLocked = ErrorCode.ERR_WithoutException + PackageType.ActorLocation * 1000 + 1; // 200003001

        /// <summary>
        /// 解锁时未找到对应的位置锁。
        /// </summary>
        public const int ERR_LocationLockNotFound = ErrorCode.ERR_WithoutException + PackageType.ActorLocation * 1000 + 2; // 200003002

        /// <summary>
        /// 解锁时旧 ActorId 与加锁时的持有者不匹配。
        /// </summary>
        public const int ERR_LocationLockOwnerMismatch = ErrorCode.ERR_WithoutException + PackageType.ActorLocation * 1000 + 3; // 200003003

        /// <summary>
        /// 解锁时提供的 LockToken 与当前锁令牌不匹配。
        /// </summary>
        public const int ERR_LocationLockTokenMismatch = ErrorCode.ERR_WithoutException + PackageType.ActorLocation * 1000 + 4; // 200003004

        /// <summary>
        /// 主 Location 服务节点当前不可用。
        /// </summary>
        public const int ERR_LocationPrimaryUnavailable = ErrorCode.ERR_WithoutException + PackageType.ActorLocation * 1000 + 5; // 200003005

        /// <summary>
        /// 目标位置当前正在迁移锁定中，通知调用方稍后重试获取。
        /// </summary>
        public const int ERR_LocationGetRetry = ErrorCode.ERR_WithoutException + PackageType.ActorLocation * 1000 + 6; // 200003006

        /// <summary>
        /// 请求发送到了备（Follower）Location 节点而被拒绝（需重新路由至主节点）。
        /// </summary>
        public const int ERR_LocationFollowerRejected = ErrorCode.ERR_WithoutException + PackageType.ActorLocation * 1000 + 8; // 200003008

        /// <summary>
        /// ActorLocation 消息发送超时错误码 2。
        /// </summary>
        public const int ERR_ActorLocationSenderTimeout2 = ErrorCode.ERR_WithException + PackageType.ActorLocation * 1000 + 4; // 100003004

        /// <summary>
        /// ActorLocation 消息发送超时错误码 3。
        /// </summary>
        public const int ERR_ActorLocationSenderTimeout3 = ErrorCode.ERR_WithException + PackageType.ActorLocation * 1000 + 5; // 100003005

        /// <summary>
        /// ActorLocation 消息发送超时错误码 4。
        /// </summary>
        public const int ERR_ActorLocationSenderTimeout4 = ErrorCode.ERR_WithException + PackageType.ActorLocation * 1000 + 6; // 100003006
    }
}

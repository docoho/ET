namespace ET
{
    /// <summary>
    /// UnityBridge 本地桥接专用错误码定义。
    /// </summary>
    public static class UnityBridgeErrorCode
    {
        /// <summary>
        /// 执行成功
        /// </summary>
        public const int Success = 0;

        /// <summary>
        /// 命令行参数或 JSON 请求格式非法
        /// </summary>
        public const int InvalidCommandLine = 200000000 + PackageType.UnityBridge * 1000 + 1;

        /// <summary>
        /// 请求执行或等待响应超时
        /// </summary>
        public const int Timeout = 200000000 + PackageType.UnityBridge * 1000 + 2;

        /// <summary>
        /// 当前不处于 PlayMode（操作要求在运行模式下执行）
        /// </summary>
        public const int NotInPlayMode = 200000000 + PackageType.UnityBridge * 1000 + 3;

        /// <summary>
        /// 当前已处于 PlayMode（操作要求在编辑模式下执行或无需重复进入）
        /// </summary>
        public const int AlreadyInPlayMode = 200000000 + PackageType.UnityBridge * 1000 + 4;

        /// <summary>
        /// Unity 编辑器正在编译脚本中
        /// </summary>
        public const int Compiling = 200000000 + PackageType.UnityBridge * 1000 + 5;

        /// <summary>
        /// 处理器内部执行失败
        /// </summary>
        public const int HandlerFail = 100000000 + PackageType.UnityBridge * 1000 + 1;
    }
}

namespace ET
{
    /// <summary>
    /// 持久化到磁盘（pending-command.json）的延迟命令状态数据，用于在 Unity 编译或域重载（Domain Reload）后恢复未完成的命令上下文。
    /// </summary>
    [EnableClass]
    public sealed class UnityBridgeDeferredCommandState : Object
    {
        /// <summary>
        /// 原始请求命令序列化 JSON
        /// </summary>
        public string CommandJson { get; set; }

        /// <summary>
        /// 关联的 RPC 请求 ID
        /// </summary>
        public int RpcId { get; set; }

        /// <summary>
        /// 幂等键
        /// </summary>
        public string IdempotencyKey { get; set; }

        /// <summary>
        /// 超时时间（毫秒）
        /// </summary>
        public int TimeoutMs { get; set; }

        /// <summary>
        /// 命令开始执行的 UTC 毫秒时间戳
        /// </summary>
        public long StartedAt { get; set; }

    }
}

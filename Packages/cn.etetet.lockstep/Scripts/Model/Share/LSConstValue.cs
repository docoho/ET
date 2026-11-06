namespace ET
{
    /// <summary>
    /// 帧同步全局常量配置定义
    /// </summary>
    public static class LSConstValue
    {
        /// <summary>
        /// 单局房间匹配所需玩家人数
        /// </summary>
        public const int MatchCount = 1;

        /// <summary>
        /// 逻辑帧标准时间间隔（毫秒，50ms 对应每秒 20 个逻辑帧）
        /// </summary>
        public const int UpdateInterval = 50;

        /// <summary>
        /// 每秒包含的逻辑帧数量（20 帧/秒）
        /// </summary>
        public const int FrameCountPerSecond = 1000 / UpdateInterval;

        /// <summary>
        /// 录像文件中保存完整 <see cref="LSWorld"/> 关键帧快照的间隔帧数（默认每 60 秒即 1200 帧保存一次，用于回放快速跳转）
        /// </summary>
        public const int SaveLSWorldFrameCount = 60 * FrameCountPerSecond;
        
        /// <summary>
        /// 配置表包所在相对路径
        /// </summary>
        public const string ExcelPackagePath = "./Packages/cn.etetet.config";
    }
}
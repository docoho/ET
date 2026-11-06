namespace ET
{
    /// <summary>
    /// 成就进度更新事件参数
    /// </summary>
    public struct AchievementProgressEvent
    {
        /// <summary>
        /// 触发成就进度变更的角色单位 ID
        /// </summary>
        public long UnitId { get; set; }

        /// <summary>
        /// 成就配置 ID
        /// </summary>
        public int AchievementId { get; set; }

        /// <summary>
        /// 更新后的当前进度值
        /// </summary>
        public int Progress { get; set; }

        /// <summary>
        /// 成就目标最大进度值
        /// </summary>
        public int MaxProgress { get; set; }
    }

    /// <summary>
    /// 成就达成完成事件参数
    /// </summary>
    public struct AchievementCompleteEvent
    {
        /// <summary>
        /// 完成成就的角色单位 ID
        /// </summary>
        public long UnitId { get; set; }

        /// <summary>
        /// 达成的成就配置 ID
        /// </summary>
        public int AchievementId { get; set; }

        /// <summary>
        /// 达成时间戳（毫秒）
        /// </summary>
        public long CompleteTime { get; set; }
    }

    /// <summary>
    /// 成就奖励已领取事件参数
    /// </summary>
    public struct AchievementClaimedEvent
    {
        /// <summary>
        /// 领取奖励的角色单位 ID
        /// </summary>
        public long UnitId { get; set; }

        /// <summary>
        /// 领取奖励的成就配置 ID
        /// </summary>
        public int AchievementId { get; set; }

        /// <summary>
        /// 奖励领取时间戳（毫秒）
        /// </summary>
        public long ClaimTime { get; set; }
    }

    /// <summary>
    /// 击杀怪物行为事件参数（供成就系统监听统计）
    /// </summary>
    public struct KillMonsterEvent
    {
        /// <summary>
        /// 击杀怪物的角色单位 ID
        /// </summary>
        public long UnitId { get; set; }

        /// <summary>
        /// 被击杀怪物的配置 ID
        /// </summary>
        public int MonsterId { get; set; }

        /// <summary>
        /// 本次击杀数量
        /// </summary>
        public int Count { get; set; }
    }

    /// <summary>
    /// 角色等级提升事件参数（供成就系统监听统计）
    /// </summary>
    public struct LevelUpEvent
    {
        /// <summary>
        /// 升级的角色单位 ID
        /// </summary>
        public long UnitId { get; set; }

        /// <summary>
        /// 提升后的最新等级
        /// </summary>
        public int Level { get; set; }
    }

    /// <summary>
    /// 任务完成事件参数（供成就系统监听统计）
    /// </summary>
    public struct QuestCompleteEvent
    {
        /// <summary>
        /// 完成任务的角色单位 ID
        /// </summary>
        public long UnitId { get; set; }

        /// <summary>
        /// 已完成的任务配置 ID
        /// </summary>
        public int QuestId { get; set; }
    }

    /// <summary>
    /// 道具收集事件参数（供成就系统监听统计）
    /// </summary>
    public struct ItemCollectEvent
    {
        /// <summary>
        /// 收集道具的角色单位 ID
        /// </summary>
        public long UnitId { get; set; }

        /// <summary>
        /// 收集到的道具配置 ID
        /// </summary>
        public int ItemId { get; set; }

        /// <summary>
        /// 本次收集数量
        /// </summary>
        public int Count { get; set; }
    }

    /// <summary>
    /// 地图探索事件参数（供成就系统监听统计）
    /// </summary>
    public struct MapExploreEvent
    {
        /// <summary>
        /// 探索地图的角色单位 ID
        /// </summary>
        public long UnitId { get; set; }

        /// <summary>
        /// 探索的地图配置 ID
        /// </summary>
        public int MapId { get; set; }
    }
}
namespace ET.Client
{
    /// <summary>
    /// 客户端任务目标（QuestObjective）系统：提供目标完成判定、进度百分比及进度文本格式化方法。
    /// </summary>
    [EntitySystemOf(typeof(QuestObjective))]
    public static partial class QuestObjectiveSystem
    {
        [EntitySystem]
        private static void Awake(this QuestObjective self)
        {
            
        }

        [EntitySystem]
        private static void Destroy(this QuestObjective self)
        {

        }

        /// <summary>
        /// 判断该任务目标在客户端记录的数量是否已达到需求数量。
        /// </summary>
        public static bool IsFinished(this QuestObjective self)
        {
            return self.Count >= self.NeedCount;
        }

        /// <summary>
        /// 获取进度百分比
        /// </summary>
        public static float GetProgressPercent(this QuestObjective self)
        {
            if (self.NeedCount <= 0)
            {
                return 0f;
            }
            return (float)self.Count / self.NeedCount;
        }

        /// <summary>
        /// 获取进度文本
        /// </summary>
        public static string GetProgressText(this QuestObjective self)
        {
            return $"{self.Count}/{self.NeedCount}";
        }
    }
}
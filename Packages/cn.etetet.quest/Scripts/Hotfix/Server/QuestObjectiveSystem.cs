namespace ET.Server
{
    /// <summary>
    /// 服务端单个任务目标（QuestObjective）实体生命周期与进度判定系统。
    /// </summary>
    [EntitySystemOf(typeof(QuestObjective))]
    public static partial class QuestObjectiveSystem
    {
        [EntitySystem]
        private static void Awake(this QuestObjective self)
        {
            QuestObjectiveConfig questObjectiveConfig = self.GetConfig();

            self.Parent?.GetParent<QuestComponent>().QuestObjectives.Add(questObjectiveConfig.Type, self);
        }
        
        [EntitySystem]
        private static void Destroy(this QuestObjective self)
        {
        }

        /// <summary>
        /// 获取该任务目标对应的配置表数据（QuestObjectiveConfig）。
        /// </summary>
        public static QuestObjectiveConfig GetConfig(this QuestObjective self)
        {
            QuestObjectiveConfig questObjectiveConfig = self.Fiber().GetSingleton<QuestObjectiveConfigCategory>().Get((int)self.Id);
            return questObjectiveConfig;
        }

        /// <summary>
        /// 判断该任务目标的当前计数（Count）是否已达到配置要求的数量（NeedCount）。
        /// </summary>
        public static bool IsFinished(this QuestObjective self)
        {
            QuestObjectiveConfig questObjectiveConfig = self.GetConfig();
            return self.Count >= questObjectiveConfig.NeedCount;
        }
    }
}

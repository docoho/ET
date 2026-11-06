using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 服务端任务实体（Quest）生命周期与子目标管理系统。
    /// </summary>
    [EntitySystemOf(typeof(Quest))]
    public static partial class QuestSystem
    {
        [EntitySystem]
        private static void Awake(this Quest self)
        {
            QuestConfig questConfig = self.GetConfig();
            foreach (int objectiveId in questConfig.ObjectiveIds)
            {
                self.AddChildWithId<QuestObjective>(objectiveId);
            }
        }
        
        /// <summary>
        /// 获取该任务对应的配置表数据（QuestConfig）。
        /// </summary>
        public static QuestConfig GetConfig(this Quest self)
        {
            QuestConfig questConfig = self.Fiber().GetSingleton<QuestConfigCategory>().Get((int)self.Id);
            return questConfig;
        }

        /// <summary>
        /// 检查该任务下属的所有任务目标（QuestObjective）是否均已完成，可否提交。
        /// </summary>
        public static bool CanSubmit(this Quest self)
        {
            foreach (KeyValuePair<long, Entity> pair in self.Children)
            {
                QuestObjective objective = (QuestObjective)pair.Value;
                if (!objective.IsFinished())
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 查找指定目标Id的任务目标
        /// </summary>
        public static QuestObjective GetQuestObjective(this Quest self, int objectiveId)
        {
            return self.GetChild<QuestObjective>(objectiveId);
        }
        
        /// <summary>
        /// 获取该任务下属于指定目标类型（QuestObjectiveType）的所有任务目标列表。
        /// </summary>
        public static List<QuestObjective> GetQuestObjectiveByType(this Quest self, QuestObjectiveType type)
        {
            List<QuestObjective> list = new();
            foreach (KeyValuePair<long, Entity> pair in self.Children)
            {
                QuestObjective objective = (QuestObjective)pair.Value;
                QuestObjectiveConfig config = objective.GetConfig();
                if (config.Type == type)
                {
                    list.Add(objective);
                }
            }
            return list;
        }
    }
}

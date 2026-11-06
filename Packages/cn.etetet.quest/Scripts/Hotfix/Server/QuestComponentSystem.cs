using System;
using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 服务端玩家任务组件系统：管理玩家已接取任务、已完成任务集合以及按目标类型索引的活跃任务目标（QuestObjectives）。
    /// </summary>
    [EntitySystemOf(typeof(QuestComponent))]
    public static partial class QuestComponentSystem
    {
        [EntitySystem]
        private static void Destroy(this QuestComponent self)
        {
        }
        
        [EntitySystem]
        private static void Awake(this QuestComponent self)
        {
        }

        /// <summary>
        /// 获取指定目标类型（如击杀怪物、收集物品）下正在追踪的所有任务目标集合。
        /// </summary>
        public static HashSet<EntityRef<QuestObjective>> GetQuestObjectiveByType(this QuestComponent self, QuestObjectiveType questObjectiveType)
        {
            HashSet<EntityRef<QuestObjective>> questObjectives;
            self.QuestObjectives.TryGetValue(questObjectiveType, out questObjectives);
            return questObjectives;
        }
        
        // 获取任务
        public static Quest GetQuest(this QuestComponent self, long questId)
        {
            return self.GetChild<Quest>(questId);
        }

        // 判断前置任务是否完成
        public static bool IsPreQuestFinished(this QuestComponent self, long questId)
        {
            QuestConfig questConfig = self.Fiber().GetSingleton<QuestConfigCategory>().Get((int)questId);
            foreach (int preQuestId in questConfig.PreQuestIds)
            {
                if (!self.FinishedQuests.Contains(preQuestId))
                {
                    return false;
                }
            }
            return true;
        }
        
        /// <summary>
        /// 为玩家添加指定 ID 的新任务，并将其下属的所有任务目标注册到类型索引表（QuestObjectives）中。
        /// </summary>
        public static Quest AddQuest(this QuestComponent self, long questId)
        {
            Quest quest = self.AddChildWithId<Quest>(questId);

            foreach (KeyValuePair<long, Entity> kv in quest.Children)
            {
                QuestObjective questObjective = kv.Value as QuestObjective;
                self.QuestObjectives.Add(questObjective.GetConfig().Type, questObjective);
            }
            
            return quest;
        }

        // 尝试完成任务
        public static bool TrySubmitQuest(this QuestComponent self, long questId)
        {
            Quest quest = self.GetQuest(questId);
            if (!quest.CanSubmit())
            {
                return false;
            }

            self.FinishedQuests.Add((int)quest.Id);
            
            self.RemoveQuest(questId);
            return true;
        }

        /// <summary>
        /// 放弃任务
        /// </summary>
        public static void RemoveQuest(this QuestComponent self, long questId)
        {
            Quest quest = self.GetChild<Quest>(questId);
            
            // 检查所有目标是否完成
            // 检查所有目标是否完成
            foreach (var kv in quest.Children)
            {
                QuestObjective questObjective = kv.Value as QuestObjective;
                if (questObjective == null)
                {
                    continue;
                }

                QuestObjectiveConfig questObjectiveConfig = questObjective.GetConfig();
                self.QuestObjectives.Remove(questObjectiveConfig.Type, questObjective);
            }
            
            self.RemoveChild(quest.Id);
        }
    }
}

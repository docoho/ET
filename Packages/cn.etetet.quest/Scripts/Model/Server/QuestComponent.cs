using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace ET.Server
{
    /// <summary>
    /// 服务端玩家任务管理组件：挂载在玩家 Unit 上，存储已完成的任务 ID 集合与正在进行的任务实体。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class QuestComponent: Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 已完成的任务Id集合
        /// </summary>
        public HashSet<int> FinishedQuests = new();

        /// <summary>
        /// 按任务目标类型（QuestObjectiveType）分类索引的活跃任务目标多值集合
        /// </summary>
        public MultiMapSet<QuestObjectiveType, EntityRef<QuestObjective>> QuestObjectives = new();
    }
}
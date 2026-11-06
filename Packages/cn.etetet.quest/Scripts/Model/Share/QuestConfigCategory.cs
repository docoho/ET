using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 任务配置表分类（QuestConfigCategory）分部扩展：在配置加载完成后构建 NPC ID 到可接/可交任务 ID 集合的反向索引。
    /// </summary>
    public partial class QuestConfigCategory
    {
        /// <summary>接取 NPC ID -> 任务 ID 集合映射</summary>
        private MultiMapSet<long, int> acceptQuest = new();
        /// <summary>提交 NPC ID -> 任务 ID 集合映射</summary>
        private MultiMapSet<long, int> submitQuest = new();
        /// <summary>NPC ID -> 该 NPC 关联的所有（可接+可交）任务 ID 集合映射</summary>
        private MultiMapSet<long, int> allQuest = new();
        
        partial void EndRef()
        {
            foreach (var kv in this.GetAll())
            {
                QuestConfig questConfig = kv.Value;
                this.acceptQuest.Add(questConfig.AcceptNPC, questConfig.Id);
                this.allQuest.Add(questConfig.AcceptNPC, questConfig.Id);
                
                this.submitQuest.Add(questConfig.SubmitNPC, questConfig.Id);
                this.allQuest.Add(questConfig.SubmitNPC, questConfig.Id);
            }
        }
        
        /// <summary>
        /// 获取可在指定 NPC（unitId）处接取的所有任务 ID 集合。
        /// </summary>
        public HashSet<int> GetAcceptQuestsById(long unitId)
        {
            HashSet<int> questIds;
            this.acceptQuest.TryGetValue(unitId, out questIds);
            return questIds;
        }
        
        /// <summary>
        /// 获取可在指定 NPC（unitId）处提交的所有任务 ID 集合。
        /// </summary>
        public HashSet<int> GetSubmitQuestsById(long unitId)
        {
            HashSet<int> questIds;
            this.submitQuest.TryGetValue(unitId, out questIds);
            return questIds;
        }
        
        /// <summary>
        /// 获取与指定 NPC（unitId）相关的所有任务 ID 集合。
        /// </summary>
        public HashSet<int> GetAllQuestsById(long unitId)
        {
            HashSet<int> questIds;
            this.allQuest.TryGetValue(unitId, out questIds);
            return questIds;
        }
    }
}

using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 服务端任务目标实体（挂载在 Quest 下，Id 即为 QuestObjectiveConfig.Id）。
    /// </summary>
    [ChildOf(typeof(Quest))]
    public class QuestObjective: Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 完成数量
        /// </summary>
        public int Count;
    }
}
using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 服务端任务实体（Id 即为任务配置表 QuestConfig.Id）。
    /// </summary>
    [ChildOf(typeof(QuestComponent))]
    public class Quest: Entity, IAwake
    {
        /// <summary>
        /// 任务当前状态
        /// </summary>
        public QuestStatus Status;
    }
}
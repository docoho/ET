namespace ET.Server
{
    /// <summary>
    /// 任务目标处理器标记特性：用于将任务目标处理器绑定到指定的 QuestObjectiveType。
    /// </summary>
    public class QuestObjectiveAttribute: BaseAttribute
    {
        /// <summary>对应的任务目标类型枚举</summary>
        public QuestObjectiveType QuestObjectiveType;
        
        public QuestObjectiveAttribute(QuestObjectiveType questObjectiveType)
        {
            this.QuestObjectiveType = questObjectiveType;
        }
    }
}
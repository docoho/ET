namespace ET
{
    /// <summary>
    /// AI 条件节点配置：检查宠物与主人距离是否超过跟随阈值且无战斗目标，判断是否应进入跟随主人状态。
    /// </summary>
    public class AI_PetFollowCheck: BTCondition
    {
        /// <summary>输入黑板变量名：承载宠物 AI 的 Buff</summary>
        [BTInput(typeof(Buff))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Buff;
    }
}

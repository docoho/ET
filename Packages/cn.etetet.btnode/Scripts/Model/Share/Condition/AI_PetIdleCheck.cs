namespace ET
{
    /// <summary>
    /// AI 条件节点配置：检查宠物是否处于主人身边且无战斗目标，判断是否应保持待机状态。
    /// </summary>
    public class AI_PetIdleCheck: BTCondition
    {
        /// <summary>输入黑板变量名：承载宠物 AI 的 Buff</summary>
        [BTInput(typeof(Buff))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Buff;
    }
}

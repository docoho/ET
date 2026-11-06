namespace ET
{
    /// <summary>
    /// AI 条件节点配置：检查宠物在战斗中是否远离主人超出最大追击距离，判断是否需要强行脱战返回主人身边。
    /// </summary>
    public class AI_PetReturnCheck: BTCondition
    {
        /// <summary>输入黑板变量名：承载宠物 AI 的 Buff</summary>
        [BTInput(typeof(Buff))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Buff;
    }
}

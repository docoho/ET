namespace ET
{
    /// <summary>
    /// AI 条件节点配置：检查宠物或主人是否存在有效仇恨目标，判断是否应进入追击战斗状态。
    /// </summary>
    public class AI_PetZhuiJiCheck: BTCondition
    {
        /// <summary>输入黑板变量名：承载宠物 AI 的 Buff</summary>
        [BTInput(typeof(Buff))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Buff;
    }
}

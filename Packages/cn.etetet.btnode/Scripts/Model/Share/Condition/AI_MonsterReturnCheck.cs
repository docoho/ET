namespace ET
{
    /// <summary>
    /// AI 条件节点配置：检查怪物是否超出追击范围或失去目标而需要脱离战斗返回出生点。
    /// </summary>
    public class AI_MonsterReturnCheck: BTCondition
    {
        /// <summary>输入黑板变量名：承载怪物 AI 的 Buff</summary>
        [BTInput(typeof(Buff))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Buff;
    }
}

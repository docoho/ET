namespace ET
{
    /// <summary>
    /// 动作节点配置：为目标单位添加指定的位面（PhaseType），使其可观测该位面中的单位。
    /// </summary>
    public class BTPhaseAdd: BTAction
    {
        /// <summary>输入黑板变量名：要添加位面的单位</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Unit;
        
        /// <summary>要添加的位面类型枚举</summary>
        public PhaseType phaseType;
    }
}
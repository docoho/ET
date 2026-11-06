namespace ET
{
    /// <summary>
    /// 动作节点配置：从目标单位移除指定的位面（PhaseType）。
    /// </summary>
    public class BTPhaseRemove: BTAction
    {
        /// <summary>输入黑板变量名：要移除位面的单位</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Unit;
        
        /// <summary>要移除的位面类型枚举</summary>
        public PhaseType phaseType;
    }
}
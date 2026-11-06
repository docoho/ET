namespace ET
{
    /// <summary>
    /// 动作节点配置：修改目标单位的指定数值属性（并将修改量记录在关联的 Buff.NumericChanges 中，以便 Buff 结束时自动回滚恢复）。
    /// </summary>
    public class BTNumericChange : BTAction
    {
        /// <summary>输入黑板变量名：要修改属性的目标单位</summary>
        [BTInput(typeof(Unit))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Unit;
        
        /// <summary>输入黑板变量名：关联的 Buff（用于记录属性变更并在 Buff 移除时还原）</summary>
        [BTInput(typeof(Buff))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Buff;
        
        /// <summary>要修改的数值属性类型</summary>
        [Sirenix.OdinInspector.LabelText("数值类型")]
        public NumericType NumericType;
        
        /// <summary>属性增减变化量</summary>
        [Sirenix.OdinInspector.LabelText("数值")]
        public int Value;

    }
}

namespace ET
{
    /// <summary>
    /// 动作节点配置：技能施放前检查或实际扣除施法者的指定数值属性（如魔法值、怒气、能量等）。
    /// </summary>
    public class BTCostNumeric: BTAction
    {
        /// <summary>输入黑板变量名：施法者单位（默认为 "Caster"）</summary>
        [BTInput(typeof(Unit))]
        public string Caster = "Caster";
        
        /// <summary>输入黑板变量名：是否为仅检查模式（true 时仅校验数值是否足够而不扣除，false 时实际扣除）</summary>
        [BTInput(typeof(bool))]
        public string Check;
        
        /// <summary>要检查或扣除的数值属性类型</summary>
        [Sirenix.OdinInspector.LabelText("数值类型")]
        public NumericType NumericType;
        
        /// <summary>需要消耗的数值大小</summary>
        public long Value;
        
    }
}

namespace ET
{
    /// <summary>
    /// 动作节点配置：施法者对目标单位造成指定数值的伤害，并维护仇恨列表。
    /// </summary>
    public class BTDamage: BTAction
    {
        /// <summary>输入黑板变量名：造成伤害的施法者单位</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Caster;
        
        /// <summary>输入黑板变量名：承受伤害的目标单位</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Target;
        
        /// <summary>输入黑板变量名：触发伤害的源 Buff（用于读取技能修正值及叠加层数）</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Buff))]
        public string Buff;
        
        /// <summary>基础伤害数值</summary>
        public int Value;
    }
}
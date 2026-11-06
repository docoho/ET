namespace ET
{
    /// <summary>
    /// 动作节点配置：对目标单位的指定技能应用属性修正器（SpellMod，如缩短冷却、增加伤害等，随关联 Buff 生命周期自动管理）。
    /// </summary>
    public class BTSpellMod : BTAction
    {
        /// <summary>输入黑板变量名：拥有技能的目标单位</summary>
        [BTInput(typeof(Unit))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Unit;
        
        /// <summary>输入黑板变量名：提供修正效果的源 Buff</summary>
        [BTInput(typeof(Buff))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Buff;
        
        /// <summary>被修正的目标技能配置 ID</summary>
        public int SpellId;
        /// <summary>技能修正器类型（如伤害修正、冷却修正等）</summary>
        public SpellModType SpellModType;
        /// <summary>修正数值</summary>
        public int Value;
    }
}
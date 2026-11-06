namespace ET
{
    /// <summary>
    /// 动作节点配置：为指定单位动态学习/添加一个新的技能（Spell），并绑定到源 Buff 生命周期。
    /// </summary>
    public class BTCreateSpell : BTAction
    {
        /// <summary>输入黑板变量名：获得新技能的目标单位</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Unit;
        
        /// <summary>输入黑板变量名：关联的源 Buff（当该 Buff 移除时可联动移除此技能）</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Buff))]
        public string Buff;
        
        /// <summary>要创建的技能配置表 ID</summary>
        public int SpellConfigId;
    }
}
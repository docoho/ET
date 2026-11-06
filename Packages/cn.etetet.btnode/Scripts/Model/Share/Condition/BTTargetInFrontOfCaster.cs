namespace ET
{
    /// <summary>
    /// 条件节点配置：检查目标单位是否位于施法者前方（且在指定技能配置的有效距离内）。
    /// </summary>
    public class BTTargetInFrontOfCaster : BTCondition
    {
        /// <summary>输入黑板变量名：施法者单位</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Caster;
        
        /// <summary>输入黑板变量名：目标单位</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Target;
        
        /// <summary>关联的技能配置表 ID</summary>
        public int SpellConfigId;
    }
}
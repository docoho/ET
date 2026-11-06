namespace ET
{
    /// <summary>
    /// 动作节点配置：在场景中创建与 Buff 关联的召唤物/光环陷阱单位（Unit），并将创建的单位输出到黑板。
    /// </summary>
    public class BTCreateBuffUnit: BTAction
    {
        /// <summary>输入黑板变量名：施法者单位</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Caster;
        
        /// <summary>输入黑板变量名：目标单位</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Target;
        
        /// <summary>输入黑板变量名：触发创建的源 Buff（新创建的 Unit 会记录在 Buff.Units 中以便联动清理）</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Buff))]
        public string Buff;
        
        /// <summary>输出黑板变量名：新创建的召唤物 Unit 实例</summary>
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTInput(typeof(Unit))]
        public string Unit;
        
        /// <summary>要创建的 Unit 配置表 ID</summary>
        public int UnitConfigId;
    }
}
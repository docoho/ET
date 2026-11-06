namespace ET
{
    /// <summary>
    /// 动作节点配置：向目标单位添加指定配置 ID 的 Buff（如果由父级 Buff 触发，会自动关联为子 Buff）。
    /// </summary>
    public class BTAddBuff: BTAction
    {
        /// <summary>输入黑板变量名：施法者单位（Caster）</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Caster;
        
        /// <summary>输入黑板变量名：承受 Buff 的目标单位（Target）</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Target;
        
        /// <summary>输入黑板变量名：触发该节点的源 Buff（可选，非空时将新 Buff 注册为其子 Buff）</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Buff))]
        public string Buff;
        
        /// <summary>要添加的 Buff 配置表 ID</summary>
        public int ConfigId;
    }
}
namespace ET
{
    /// <summary>
    /// 动作节点配置：使施法者朝向黑板中指定的目标单位（Target）。
    /// </summary>
    public class BTTurnToUnit: BTAction
    {
        /// <summary>输入黑板变量名：需要转向的施法者单位</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Caster;
        
        /// <summary>输入黑板变量名：朝向的目标单位</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Target;
        
        /// <summary>输入黑板变量名：关联的源 Buff</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Buff))]
        public string Buff;
    }
}
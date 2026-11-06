namespace ET
{
    /// <summary>
    /// 动作节点配置：为施法者召唤创建宠物单位（Pet），并将创建的宠物实例输出到黑板。
    /// </summary>
    public class BTCreatePet: BTAction
    {
        /// <summary>输入黑板变量名：召唤者（主人）单位</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Caster;
        
        /// <summary>输入黑板变量名：目标单位</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Target;
        
        /// <summary>输入黑板变量名：触发召唤的源 Buff</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Buff))]
        public string Buff;
        
        /// <summary>输出黑板变量名：新创建的宠物 Unit 实例</summary>
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTInput(typeof(Unit))]
        public string Pet;
        
        /// <summary>宠物对应的 Unit 配置表 ID</summary>
        public int UnitConfigId;
    }
}
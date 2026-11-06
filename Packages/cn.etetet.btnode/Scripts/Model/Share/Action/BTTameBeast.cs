namespace ET
{
    /// <summary>
    /// 动作节点配置：驯服野兽（将目标怪物转化为施法者的宠物，并切换为宠物 AI 行为树）。
    /// </summary>
    public class BTTameBeast : BTAction
    {
        /// <summary>输入黑板变量名：施放驯服技能的主人单位</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Unit;
        
        /// <summary>输入黑板变量名：被驯服的目标野兽单位</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Target;
        
        /// <summary>输入黑板变量名：触发驯服逻辑的源 Buff</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Buff))]
        public string Buff;

        /// <summary>驯服成为宠物后挂载的宠物 AI Buff 配置表 ID</summary>
        public int AIConfigId;
    }
}
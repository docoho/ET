namespace ET
{
    /// <summary>
    /// 动作节点配置：从目标单位移除指定配置 ID 或符合指定标签（BuffFlags）的 Buff。
    /// </summary>
    public class BTRemoveBuff: BTAction
    {
        /// <summary>输入黑板变量名：要移除 Buff 的目标单位</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Unit;
        
        /// <summary>要移除的 Buff 配置表 ID</summary>
        public int ConfigId;

        /// <summary>Buff 移除原因或匹配标签（BuffFlags）</summary>
        public BuffFlags RemoveType;
    }
}
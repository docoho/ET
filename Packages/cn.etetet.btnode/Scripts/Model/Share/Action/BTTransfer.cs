namespace ET
{
    /// <summary>
    /// 动作节点配置：将目标单位按传送配置 ID（TransferConfig）传送到目标位置或跨地图传送。
    /// </summary>
    public class BTTransfer: BTAction
    {
        /// <summary>输入黑板变量名：施法者单位</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Caster;
        
        /// <summary>输入黑板变量名：被传送的目标单位</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Target;
        
        /// <summary>输入黑板变量名：关联的源 Buff</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Buff))]
        public string Buff;
        
        /// <summary>传送点配置表 ID（TransferConfig）</summary>
        public int TransferId;
    }
}
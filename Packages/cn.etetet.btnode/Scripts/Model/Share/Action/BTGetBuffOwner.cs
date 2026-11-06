namespace ET
{
    /// <summary>
    /// 动作节点配置：获取指定 Buff 的挂载持有者（Owner）单位并写入黑板。
    /// </summary>
    public class BTGetBuffOwner: BTAction
    {
        /// <summary>输入黑板变量名：要查询的 Buff</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Buff))]
        public string Buff;
        
        /// <summary>输出黑板变量名：获取到的 Buff 持有者 Unit</summary>
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(Unit))]
        public string Unit;
    }
}
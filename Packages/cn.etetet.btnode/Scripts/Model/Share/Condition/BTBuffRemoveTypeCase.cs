namespace ET
{
    /// <summary>
    /// 条件节点配置：在 Buff 移除事件（BuffRemove）中判断 Buff 的实际移除原因是否包含指定的标签（BuffFlags）。
    /// </summary>
    public class BTBuffRemoveTypeCase: BTCondition
    {
        /// <summary>输入黑板变量名：正在被移除的 Buff</summary>
        [BTInput(typeof(Buff))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Buff;
        
        /// <summary>期望匹配的 Buff 移除类型标志位（如正常过期、被驱散、主动取消等）</summary>
        public BuffFlags BuffRemoveType;
    }
}
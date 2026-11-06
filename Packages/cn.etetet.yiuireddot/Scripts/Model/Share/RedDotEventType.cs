namespace ET
{
    /// <summary>
    /// 逻辑层通知视图层更新指定红点数量的事件参数结构体。
    /// </summary>
    public struct Event_RedDot_Change
    {
        /// <summary>目标红点枚举Key（ERedDotKeyType）。</summary>
        public int RedDotId;
        /// <summary>最新红点数量。</summary>
        public int Count;
    }
}
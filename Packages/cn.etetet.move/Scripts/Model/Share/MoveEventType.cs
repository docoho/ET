namespace ET
{
    /// <summary>
    /// 单位开始移动事件（表现层可订阅以播放跑步动画）。
    /// </summary>
    public struct MoveStart
    {
        /// <summary>
        /// 开始移动的单位引用。
        /// </summary>
        public EntityRef<Unit> Unit;
    }

    /// <summary>
    /// 单位停止移动事件（表现层可订阅以切回待机动画）。
    /// </summary>
    public struct MoveStop
    {
        /// <summary>
        /// 停止移动的单位引用。
        /// </summary>
        public EntityRef<Unit> Unit;
    }
}
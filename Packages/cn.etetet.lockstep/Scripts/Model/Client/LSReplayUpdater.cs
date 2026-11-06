namespace ET.Client
{
    /// <summary>
    /// 帧同步客户端录像回放更新器组件（挂载在回放模式下的客户端 <see cref="Room"/> 上）
    /// </summary>
    [ComponentOf(typeof(Room))]
    public class LSReplayUpdater: Entity, IAwake, IUpdate
    {
        /// <summary>
        /// 当前录像回放倍速（支持 1x、2x、4x、8x）
        /// </summary>
        public int ReplaySpeed { get; set; } = 1;
    }
}
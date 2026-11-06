namespace ET
{
    /// <summary>
    /// 帧同步固定时间步长计数器，用于精确计算每个逻辑帧对应的目标时间戳，并支持在任意帧动态修改单帧时间间隔（用于客户端时间膨胀/收缩与回放变速）
    /// </summary>
    public class FixedTimeCounter: Object
    {
        /// <summary>
        /// 当前速率段的基准起始时间戳（毫秒）
        /// </summary>
        private long startTime;

        /// <summary>
        /// 当前速率段的基准起始帧号
        /// </summary>
        private int startFrame;

        /// <summary>
        /// 当前单帧时间间隔（毫秒，默认 50ms 即 20FPS）
        /// </summary>
        public int Interval { get; private set; }

        /// <summary>
        /// 构造固定时间步长计数器
        /// </summary>
        /// <param name="startTime">初始基准时间戳（毫秒）</param>
        /// <param name="startFrame">初始基准帧号</param>
        /// <param name="interval">单帧时间间隔（毫秒）</param>
        public FixedTimeCounter(long startTime, int startFrame, int interval)
        {
            this.startTime = startTime;
            this.startFrame = startFrame;
            this.Interval = interval;
        }
        
        /// <summary>
        /// 在指定帧处切换新的单帧时间间隔：先将基准时间推进到 <paramref name="frame"/> 对应的时间点，再更新 <see cref="Interval"/>，保证时间轴连续无跳变
        /// </summary>
        /// <param name="interval">新的单帧时间间隔（毫秒）</param>
        /// <param name="frame">发生变速的当前帧号</param>
        public void ChangeInterval(int interval, int frame)
        {
            this.startTime += (frame - this.startFrame) * this.Interval;
            this.startFrame = frame;
            this.Interval = interval;
        }

        /// <summary>
        /// 计算指定逻辑帧号对应的目标绝对时间戳（毫秒）
        /// </summary>
        /// <param name="frame">逻辑帧号</param>
        /// <returns>该帧应当执行的目标时间戳</returns>
        public long FrameTime(int frame)
        {
            return this.startTime + (frame - this.startFrame) * this.Interval;
        }
        
        /// <summary>
        /// 重置基准时间戳与基准帧号（如录像跳转时使用）
        /// </summary>
        /// <param name="time">新的基准时间戳</param>
        /// <param name="frame">新的基准帧号</param>
        public void Reset(long time, int frame)
        {
            this.startTime = time;
            this.startFrame = frame;
        }
    }
}
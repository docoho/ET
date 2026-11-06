using System;

namespace ET
{
    /// <summary>
    /// 全局高精度跨平台时间管理单例（线程安全）。
    /// <para>
    /// 【设计亮点】
    /// 避免在每帧或每次取时间戳时频繁调用开销较大的 <see cref="DateTime.UtcNow"/>，并且免疫运行期间操作系统本地时钟被用户手动回调带来的时间倒流问题：
    /// 在启动 <see cref="Awake"/> 时记录一次基准纪元偏移 <c>tick1970</c>，后续全部基于单调递增的硬件计时器（<c>Stopwatch.GetTimestamp()</c> 或 <c>Environment.TickCount64</c>）计算毫秒级 Unix 时间戳！
    /// </para>
    /// </summary>
    public class TimeInfo: Singleton<TimeInfo>, ISingletonAwake
    {
        private int timeZone;
        
        /// <summary>当前时区偏移（小时）</summary>
        public int TimeZone
        {
            get
            {
                return this.timeZone;
            }
            set
            {
                this.timeZone = value;
            }
        }
        
        private DateTime dt1970;
        private long tick1970;
        
        /// <summary>
        /// 服务端时间减去客户端本地时间的毫秒差值（由客户端 PingComponent 在对时后动态更新）。
        /// </summary>
        // ping消息会设置该值，原子操作
        public long ServerMinusClientTime { private get; set; }
        
        public void Awake()
        {
            this.dt1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            this.tick1970 = GetTick() - (DateTime.UtcNow.Ticks - this.dt1970.Ticks) / 10000;
        }

        /// <summary>
        /// 返回单调递增的硬件运行毫秒数（跨平台，无状态，超长运行不溢出）
        /// </summary>
        private static long GetTick()
        {
#if UNITY
            // Unity 下使用 Stopwatch.GetTimestamp()
            long ticks = System.Diagnostics.Stopwatch.GetTimestamp();
            // 换算为毫秒
            return ticks * 1000 / System.Diagnostics.Stopwatch.Frequency;
#else
            // 服务端直接用 TickCount64
            return Environment.TickCount64;
#endif
        }
        
        /// <summary>
        /// 获取当前本地 Unix 毫秒时间戳（自 1970-01-01 00:00:00 UTC 起的毫秒数，线程安全）。
        /// </summary>
        // 线程安全
        public long ClientNow()
        {
            return GetTick() - this.tick1970;
        }
        
        /// <summary>
        /// 获取经过网络对时校准后的当前服务器毫秒时间戳（在服务端等同于 <see cref="ClientNow"/>；在客户端等于 <c>ClientNow() + ServerMinusClientTime</c>）。
        /// </summary>
        public long ServerNow()
        {
            return ClientNow() + this.ServerMinusClientTime;
        }
    }
}

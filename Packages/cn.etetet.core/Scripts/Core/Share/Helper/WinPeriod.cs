using System.Runtime.InteropServices;

namespace ET
{
    /// <summary>
    /// Windows 系统定时器分辨率设置工具类。
    /// <remarks>
    /// Windows 默认的系统时钟中断周期通常为 15.6ms，这会导致 <c>Thread.Sleep(1)</c> 实际休眠约 15ms；
    /// 在进程启动时调用 <see cref="Init"/> 通过 <c>winmm.dll</c> 的 <c>timeBeginPeriod(1)</c> 将系统定时器精度提升至 1ms，保证服务端主循环帧率稳定。
    /// </remarks>
    /// </summary>
    public static class WinPeriod
    {
        // 一般默认的精度不止1毫秒（不同操作系统有所不同），需要调用timeBeginPeriod与timeEndPeriod来设置精度
        [DllImport("winmm")]
        private static extern void timeBeginPeriod(int t);
        //[DllImport("winmm")]
        //static extern void timeEndPeriod(int t);

        /// <summary>
        /// 若当前运行在 Windows 平台上，则将系统定时器最小周期设为 1 毫秒。
        /// </summary>
        public static void Init()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                timeBeginPeriod(1);
            }
        }
    }
}
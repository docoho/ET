namespace ET.Server
{
    /// <summary>
    /// 防止 .NET 编译器裁剪服务端 Model 程序集的占位辅助类（由 <c>Program.Main</c> 显式调用）
    /// </summary>
    public static class AvoidCut
    {
        /// <summary>
        /// 触发程序集加载的空初始化方法
        /// </summary>
        public static void Init()
        {
        }
    }
}
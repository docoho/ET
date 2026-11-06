namespace ET
{
    /// <summary>
    /// 协程锁类型扩展定义（Transfer 传送模块）。
    /// </summary>
    public static partial class CoroutineLockType
    {
        /// <summary>
        /// 客户端场景切换流程同步协程锁。
        /// </summary>
        public const int SceneChange = PackageType.Transfer * 1000 + 1;
    }
}

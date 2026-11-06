namespace ET
{
    /// <summary>
    /// 状态同步模块场景类型标识定义
    /// </summary>
    public static partial class SceneType
    {
        // 客户端
        /// <summary>
        /// 状态同步主入口场景类型
        /// </summary>
        public const int StateSync = PackageType.StateSync * 1000 + 1;
    }
}
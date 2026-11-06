namespace ET
{
    /// <summary>
    /// 场景类型常量扩展，定义自动化测试相关的场景类型。
    /// </summary>
    public static partial class SceneType
    {
        /// <summary>
        /// 测试主控场景（运行测试控制台与用例调度器）
        /// </summary>
        public const int Test = PackageType.Test * 1000 + 1;

        /// <summary>
        /// 完整服务器测试用例场景（每个用例拉起一套独立的完整分布式服务器 Fiber 环境）
        /// </summary>
        public const int TestCase = PackageType.Test * 1000 + 2;

        /// <summary>
        /// 轻量空白测试用例场景（仅包含 Root Scene 与数据库自动清理组件，不拉起完整业务服）
        /// </summary>
        public const int TestEmpty = PackageType.Test * 1000 + 3;
    }
}
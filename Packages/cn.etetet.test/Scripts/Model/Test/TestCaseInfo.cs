namespace ET.Test
{
    /// <summary>
    /// 单个测试用例的元数据描述信息，包含用例名称、处理器实例以及执行模式（并行或独占）。
    /// </summary>
    [EnableClass]
    public sealed class TestCaseInfo
    {
        /// <summary>
        /// 测试用例处理器类名
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 测试用例处理器实例
        /// </summary>
        public ITestHandler Handler { get; }

        /// <summary>
        /// 测试用例执行并发模式（Parallel 或 Exclusive）
        /// </summary>
        public TestExecutionMode Mode { get; }

        public TestCaseInfo(string name, ITestHandler handler, TestExecutionMode mode)
        {
            this.Name = name;
            this.Handler = handler;
            this.Mode = mode;
        }
    }
}

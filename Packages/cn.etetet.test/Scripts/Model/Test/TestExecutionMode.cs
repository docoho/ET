namespace ET.Test
{
    /// <summary>
    /// 测试用例执行并发模式枚举。
    /// </summary>
    public enum TestExecutionMode
    {
        /// <summary>
        /// 允许与其他并行用例同批次并发执行（默认）
        /// </summary>
        Parallel = 0,

        /// <summary>
        /// 独占模式，必须在并行批次全部结束后单独串行执行
        /// </summary>
        Exclusive = 1,
    }
}

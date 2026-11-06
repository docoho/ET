namespace ET.Test
{
    /// <summary>
    /// 机器人测试用例回调参数
    /// </summary>
    public struct TestContext
    {
        /// <summary>
        /// 测试宿主 Fiber 实例
        /// </summary>
        public Fiber Fiber;

        /// <summary>
        /// 测试命令行参数对象
        /// </summary>
        public TestArgs Args;
    }
}
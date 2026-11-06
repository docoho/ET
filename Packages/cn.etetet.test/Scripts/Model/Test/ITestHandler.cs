namespace ET.Test
{
    /// <summary>
    /// 测试用例处理器统一接口。
    /// </summary>
    public interface ITestHandler
    {
        /// <summary>
        /// 异步执行测试用例并返回结果错误码（0 表示通过）
        /// </summary>
        ETTask<int> Handle(TestContext context);
    }
}
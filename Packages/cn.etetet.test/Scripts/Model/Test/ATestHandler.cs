namespace ET.Test
{
    /// <summary>
    /// 机器人测试用例处理器抽象基类
    /// 继承自ATestHandler，使用Invoke机制进行分发
    /// （所有具体测试用例类均需继承此类并实现 Handle 方法，返回 0 表示测试通过，非 0 表示失败错误码）
    /// </summary>
    [Test]
    public abstract class ATestHandler: HandlerObject, ITestHandler
    {
        /// <summary>
        /// 执行单个测试用例逻辑，返回错误码（0 表示成功）
        /// </summary>
        public abstract ETTask<int> Handle(TestContext context);
    }
}

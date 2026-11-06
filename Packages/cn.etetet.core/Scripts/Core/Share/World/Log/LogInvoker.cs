namespace ET
{
    /// <summary>
    /// 为新建的 <see cref="Fiber"/> 创建专属 <see cref="ILog"/> 实例的 Invoke 请求参数结构体。
    /// </summary>
    public struct LogInvoker
    {
        /// <summary>纤程/场景名称</summary>
        public string SceneName;
    }
}
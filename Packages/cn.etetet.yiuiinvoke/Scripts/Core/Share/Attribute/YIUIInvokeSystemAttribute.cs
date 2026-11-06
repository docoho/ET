namespace ET
{
    /// <summary>
    /// 由SourceGenerator自动生成在YIUIInvokeHandler包装类上的系统注册特性，供YIUIInvokeSystem在Awake时反射收集。
    /// </summary>
    public class YIUIInvokeSystemAttribute : BaseAttribute
    {
        public string InvokeType { get; }

        public YIUIInvokeSystemAttribute(string invokeType)
        {
            this.InvokeType = invokeType;
        }
    }
}
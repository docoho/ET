namespace ET
{
    //文档 https://lib9kmxvq7k.feishu.cn/wiki/XXzrwRGgti52s4kYdqFccswAnXe
    /// <summary>
    /// 由SourceGenerator自动生成在YIUI切面监听Handler包装类上的系统注册特性，供YIUIListenerInvokeSystem在Awake时反射收集。
    /// </summary>
    public class YIUIListenerInvokeSystemAttribute : BaseAttribute
    {
        public string InvokeType { get; }

        /// <summary>
        /// 执行优先级
        /// 小于0 则在Invoke之前执行
        /// 大于等于0 则在Invoke之后执行
        /// 同级越小的越先执行
        /// </summary>
        public int Priority { get; }

        public YIUIListenerInvokeSystemAttribute(string invokeType, int priority = 0)
        {
            this.InvokeType = invokeType;
            this.Priority   = priority;
        }
    }
}
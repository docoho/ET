using System;

namespace ET
{
    //文档:https://lib9kmxvq7k.feishu.cn/wiki/TpyYwbWIUizhfKkcubocTZgInse
    /// <summary>
    /// YIUI事件跨层调用标记特性（贴在HotfixView层方法上，由SourceGenerator自动生成对应的IYIUIInvokeBaseHandler派生类）。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class YIUIInvokeAttribute : BaseAttribute
    {
        public string InvokeType { get; }

        //如果有参数必须是Const
        public YIUIInvokeAttribute(string invokeType)
        {
            this.InvokeType = invokeType;
        }

        //没有参数则SG读取的是方法名
        public YIUIInvokeAttribute()
        {
        }
    }
}
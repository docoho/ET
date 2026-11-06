using System;

namespace ET
{
    /// <summary>
    /// 当方法或属性内需要直接访问 <see cref="Entity"/> 类的底层 <c>Children</c> 或 <c>Components</c> 集合时使用此标签豁免分析器告警。
    /// 仅供框架底层或特殊工具方法使用，常规业务代码应始终通过 <c>GetComponent</c> / <c>GetChild</c> 等标准 API 访问。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method|AttributeTargets.Property)]
    public class EnableAccessEntiyChildAttribute : Attribute
    {
        
    }
}


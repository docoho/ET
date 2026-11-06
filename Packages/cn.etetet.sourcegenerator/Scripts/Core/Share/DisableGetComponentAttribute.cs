using System;

namespace ET
{
    /// <summary>
    /// 标记的组件类型禁止在未授权的情况下通过 <c>GetComponent</c> 直接获取（由 <c>DisableGetComponentAnalyzer</c> 检查）。
    /// 若某个类或方法确需获取该组件，必须显式标注 <see cref="EnableGetComponentAttribute"/>。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class DisableGetComponentAttribute : Attribute
    {
    }
}

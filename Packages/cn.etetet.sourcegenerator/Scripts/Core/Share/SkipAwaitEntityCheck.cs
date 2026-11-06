using System;

namespace ET
{
    /// <summary>
    /// 跳过“跨 <c>await</c> 异步调用后直接访问 <see cref="Entity"/> 局部变量”编译期静态检查的豁免特性。
    /// 默认情况下，分析器要求在 <c>await</c> 之后访问实体前必须通过 <see cref="EntityRef{T}"/> 重新还原并判空；若能明确保证生命周期安全可标注此特性跳过检查。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class SkipAwaitEntityCheckAttribute: Attribute
    {
        
    }
}
using System;

namespace ET
{
    /// <summary>
    /// 添加该标记的类或结构体禁止在业务代码中直接使用 <c>new</c> 关键字构造对象（例如 <see cref="Entity"/> 及其子类必须通过 <c>AddComponent</c> 或 <c>AddChild</c> 创建以纳入生命周期与对象池管理）。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Struct,Inherited = true)]
    public class DisableNewAttribute : Attribute
    {
        
    }
}


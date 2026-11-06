using System;

namespace ET
{
    /// <summary>
    /// 允许在标记的方法或类型内通过 <c>GetComponent</c> 获取指定的受保护组件（即标记了 <see cref="DisableGetComponentAttribute"/> 的组件）。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class EnableGetComponentAttribute : Attribute
    {
        /// <summary>被授权允许通过 <c>GetComponent</c> 获取的组件类型。</summary>
        public Type ComponentType { get; }

        public EnableGetComponentAttribute(Type componentType)
        {
            this.ComponentType = componentType;
        }
    }
}

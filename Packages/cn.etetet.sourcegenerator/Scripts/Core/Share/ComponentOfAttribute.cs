using System;

namespace ET
{
    /// <summary>
    /// 组件类父级实体类型约束（由 <c>EntityComponentAnalyzer</c> 在编译期静态检查）。
    /// 父级实体类型唯一的，标记指定父级实体类型 <c>[ComponentOf(typeof(ParentType))]</c>；
    /// 若可挂载在任意实体上（不唯一），则标记无参的 <c>[ComponentOf]</c>。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ComponentOfAttribute : Attribute
    {
        /// <summary>允许挂载该组件的唯一父实体类型；若为 null 则表示允许挂载到任意实体上。</summary>
        public Type Type;

        public ComponentOfAttribute(Type type = null)
        {
            this.Type = type;
        }
    }
}
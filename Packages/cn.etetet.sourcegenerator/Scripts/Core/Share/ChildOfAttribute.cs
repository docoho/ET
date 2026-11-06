using System;

namespace ET
{
    /// <summary>
    /// 子实体的父级实体类型约束（由 <c>AddChildTypeAnalyzer</c> 在编译期静态检查）。
    /// 父级实体类型唯一的，标记指定父级实体类型 <c>[ChildOf(typeof(ParentType))]</c>；
    /// 若可作为多种父实体的子实体（不唯一），则标记无参的 <c>[ChildOf]</c>。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ChildOfAttribute : Attribute
    {
        /// <summary>允许挂载的唯一父实体类型；若为 null 则表示允许作为任意实体的子实体（Child）。</summary>
        public Type type;

        public ChildOfAttribute(Type type = null)
        {
            this.type = type;
        }
    }
}
using System;

namespace ET
{
    // 在您的项目中添加以下特性，用于标记允许引用 Entity 的字段或属性：
    /// <summary>
    /// 标记允许直接持有 <see cref="Entity"/>（或其派生类）引用的字段或属性。
    /// 默认情况下，ET 的 Roslyn 分析器（<c>EntityMemberAnalyzer</c>）禁止在类中直接声明 <c>Entity</c> 类型的字段，强制要求使用 <see cref="EntityRef{T}"/> 或 <see cref="EntityWeakRef{T}"/> 防止异步悬空引用；
    /// 仅当框架底层机制确需直接持有 <c>Entity</c> 引用且生命周期受控时，才标注此特性豁免检查。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class AllowEntityMemberAttribute : Attribute
    {
    }
}
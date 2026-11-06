using System;

namespace ET
{
    /// <summary>
    /// 行为树整型下拉选择特性，允许在行为树编辑器中通过指定的枚举单例类型（如 NumericType）将 int 字段渲染为带搜索功能的下拉列表。
    /// </summary>
    [EnableClass]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class BTIntDropdownAttribute : Attribute
    {
        public BTIntDropdownAttribute(Type enumSingletonType)
        {
            this.EnumSingletonType = enumSingletonType;
        }

        public Type EnumSingletonType { get; }
    }
}

using System;

namespace ET
{
    /// <summary>
    /// 标识类型所属的逻辑模块名称特性。
    /// 供 <c>ClassCircularDependencyAnalyzer</c> 在编译期构建模块依赖有向图，静态检测不同业务模块之间是否出现了非法的循环依赖。
    /// </summary>
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    public class ModuleAttribute : Attribute
    {
        /// <summary>所属模块名称。</summary>
        public string Name { get; }
        public ModuleAttribute(string name)
        {
            Name = name;
        }
    }
}
using System;

namespace ET
{
    /// <summary>
    /// 忽略模块间循环依赖检查的豁免特性（配合 <c>ClassCircularDependencyAnalyzer</c> 使用）。
    /// 当某个类被标记此特性时，分析器不再检查该类对其他模块（<see cref="ModuleAttribute"/>）的反向依赖。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class IgnoreCircularDependencyAttribute: Attribute
    {
        
    }
}
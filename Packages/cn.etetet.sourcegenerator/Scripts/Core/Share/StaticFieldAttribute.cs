using System;

namespace ET
{
    /// <summary>
    /// 允许声明静态字段或静态属性的豁免标签（配合 <c>StaticFieldDeclarationAnalyzer</c> 使用）。
    /// 因为 ET 支持单进程多纤程（<see cref="Fiber"/>）并发运行（如单进程同时跑多个区的服务端或多个机器人），随意使用未受控的静态字段会导致跨线程数据竞争与状态污染；
    /// 因此除只读配置或线程安全基础设施外，声明静态字段必须显式加上 <c>[StaticField]</c> 标签以表明开发者知晓其多线程安全性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class StaticFieldAttribute: Attribute
    {
    }
}


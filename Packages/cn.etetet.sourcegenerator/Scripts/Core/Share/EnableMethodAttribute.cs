using System;

namespace ET
{
    /// <summary>
    /// 对于特殊实体类，允许在 <see cref="Entity"/> 子类内部直接声明方法的豁免标签。
    /// 默认情况下，ET 的 ECS 规范要求 <c>Entity</c> 派生类纯存数据、所有逻辑方法必须写在 Hotfix 层的 <c>*System</c> 扩展类中（由 <c>EntityMethodDeclarationAnalyzer</c> 检查）；
    /// 仅当框架底层实体确需在类内定义方法时标注此特性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class EnableMethodAttribute : Attribute
    {
        
    }
}
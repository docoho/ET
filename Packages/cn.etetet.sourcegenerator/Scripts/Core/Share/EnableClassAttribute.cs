using System;

namespace ET
{
    /// <summary>
    /// 允许在 Model 或 Hotfix 程序集中声明普通 C# 类的标记特性（或其基类特性）。
    /// ET 架构默认要求 Model 层只定义 Entity/Component 数据类、Hotfix 层只定义静态扩展方法类，
    /// 若确需定义辅助数据结构或内部状态类，需标注 <c>[EnableClass]</c> 以通过 <c>DisableNormalClassDeclaratonInModelAssemblyAnalyzer</c> / <c>ClassDeclarationInHotfixAnalyzer</c> 检查。
    /// </summary>
    public class EnableClassAttribute: Attribute
    {
    }
}
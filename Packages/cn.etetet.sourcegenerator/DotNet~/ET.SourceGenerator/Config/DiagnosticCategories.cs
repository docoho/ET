namespace ET;

/// <summary>
/// Roslyn 诊断规则（DiagnosticDescriptor）所属的分类常量。
/// </summary>
public static class DiagnosticCategories
{
    /// <summary>源码生成器相关分析器分类。</summary>
    public const string Generator = "ETGeneratorAnalyzers";
    /// <summary>Hotfix/HotfixView 热更项目专用分析器分类。</summary>
    public const string Hotfix = "ETHotfixProjectAnalyzers";
    /// <summary>Model/ModelView 模型项目专用分析器分类。</summary>
    public const string Model = "ETModelProjectAnalyzers";
    /// <summary>全项目通用分析器分类。</summary>
    public const string All = "ETAllProjectAnalyzers";
}
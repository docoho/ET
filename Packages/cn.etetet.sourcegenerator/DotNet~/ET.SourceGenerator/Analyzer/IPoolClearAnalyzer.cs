using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ET;

/// <summary>
/// <c>IPool</c> 对象池类型可清理性检查分析器（ET0040）。
/// <para>与 <see cref="IPoolClearGenerator"/> 配合：若非抽象类直接实现了 <c>ET.IPool</c>，但既没有手写显式 <c>void IPool.Clear()</c> 实现，也不满足自动生成条件（例如未声明为 <c>partial</c>、属于嵌套类、或包含需要手动清理的 <c>IDisposable</c>/不可自动清空集合字段），则报错提示开发者补全。</para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class IPoolClearAnalyzer: DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(IPoolClearAnalyzerRule.Rule);

    public override void Initialize(AnalysisContext context)
    {
        if (!AnalyzerGlobalSetting.EnableAnalyzer)
        {
            return;
        }

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(analysisContext =>
        {
            if (AnalyzerHelper.IsAssemblyNeedAnalyze(analysisContext.Compilation.AssemblyName, AnalyzeAssembly.All))
            {
                analysisContext.RegisterSymbolAction(this.Analyze, SymbolKind.NamedType);
            }
        });
    }

    private void Analyze(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol typeSymbol)
        {
            return;
        }

        if (typeSymbol.TypeKind != TypeKind.Class || typeSymbol.IsAbstract)
        {
            return;
        }

        if (!IPoolClearGenerator.ImplementsIPoolDirectly(typeSymbol))
        {
            return;
        }

        if (IPoolClearGenerator.HasExplicitIPoolClear(typeSymbol))
        {
            return;
        }

        foreach (SyntaxReference declaringSyntaxReference in typeSymbol.DeclaringSyntaxReferences)
        {
            if (declaringSyntaxReference.GetSyntax(context.CancellationToken) is not ClassDeclarationSyntax classDeclarationSyntax)
            {
                continue;
            }

            if (classDeclarationSyntax.IsPartial() && typeSymbol.ContainingType == null &&
                !IPoolClearGenerator.NeedsManualClear(typeSymbol))
            {
                return;
            }

            Diagnostic diagnostic = Diagnostic.Create(
                IPoolClearAnalyzerRule.Rule,
                classDeclarationSyntax.Identifier.GetLocation(),
                typeSymbol.Name);
            context.ReportDiagnostic(diagnostic);
            return;
        }
    }
}

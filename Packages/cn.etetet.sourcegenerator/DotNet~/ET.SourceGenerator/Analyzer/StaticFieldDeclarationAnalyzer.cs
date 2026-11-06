using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ET
{
    /// <summary>
    /// 静态字段与静态属性强制标记 <c>[StaticField]</c> 分析器（ET0015）。
    /// <para>由于 ET 采用多 Fiber（可能运行在多线程）架构，全局静态变量极易引发跨 Fiber 并发竞争与热重载残留问题；因此强制要求 <c>ET</c> 与 <c>YIUIFramework</c> 命名空间下的所有非 <c>const</c> 静态字段及静态属性必须显式标记 <c>[StaticField]</c> 以示审阅确认。</para>
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class StaticFieldDeclarationAnalyzer : DiagnosticAnalyzer
    {
        
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>ImmutableArray.Create(StaticFieldDeclarationAnalyzerRule.Rule);
        
        public override void Initialize(AnalysisContext context)
        {
            if (!AnalyzerGlobalSetting.EnableAnalyzer)
            {
                return;
            }
            
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(this.Analyzer, SymbolKind.NamedType);
        }
        
        private void Analyzer(SymbolAnalysisContext context)
        {
            if (!AnalyzerHelper.IsAssemblyNeedAnalyze(context.Compilation.AssemblyName, AnalyzeAssembly.All))
            {
                return;
            }

            if (!(context.Symbol is INamedTypeSymbol namedTypeSymbol))
            {
                return;
            }

            // 【扩展检查范围】：检查ET命名空间和YIUIFramework命名空间
            string? namespaceName = namedTypeSymbol.ContainingNamespace?.ToDisplayString();
            bool shouldAnalyze = namespaceName == "ET" || 
                               namespaceName?.StartsWith("ET.") == true ||
                               namespaceName == "YIUIFramework" ||
                               namespaceName?.StartsWith("YIUIFramework.") == true;
            
            if (!shouldAnalyze)
            {
                return;
            }

            foreach (ISymbol? memberSymbol in namedTypeSymbol.GetMembers())
            {
                if (memberSymbol is IFieldSymbol { IsConst: false,IsStatic:true } or IPropertySymbol { IsStatic: true })
                {
                    bool hasAttr = memberSymbol.GetAttributes().Any(x => x.AttributeClass?.ToString() == Definition.StaticFieldAttribute);
                    if (!hasAttr)
                    {
                        ReportDiagnostic(memberSymbol);
                    }
                }
            }

            void ReportDiagnostic(ISymbol symbol)
            {
                foreach (SyntaxReference? declaringSyntaxReference in symbol.DeclaringSyntaxReferences)
                {
                    Diagnostic diagnostic = Diagnostic.Create(StaticFieldDeclarationAnalyzerRule.Rule, declaringSyntaxReference.GetSyntax()?.GetLocation(), symbol.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
        
    }
}


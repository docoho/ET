using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ET
{
    /// <summary>
    /// Model / ModelView 程序集禁止声明普通非实体类分析器（ET0032）。
    /// <para>在数据模型层程序集中，要求所有非静态类必须继承自带有 <c>[EnableClass]</c> 的框架基类（如 <c>Object</c>、<c>Entity</c>、<c>ASingleton</c> 等）或自身显式标记 <c>[EnableClass]</c>，以保证 ECS 架构纯粹性。</para>
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DisableNormalClassDeclaratonInModelAssemblyAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(EntityClassDeclarationAnalyzerrRule.Rule);

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
                if (AnalyzerHelper.IsAssemblyNeedAnalyze(analysisContext.Compilation.AssemblyName, AnalyzeAssembly.AllModel))
                {
                    analysisContext.RegisterSyntaxNodeAction(Analyze,SyntaxKind.ClassDeclaration);
                }
            });
            
        }
        
        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not ClassDeclarationSyntax classDeclarationSyntax)
            {
                return;
            }

            var namedTypeSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);

            if (namedTypeSymbol==null)
            {
                return;
            }

            if (namedTypeSymbol.IsStatic)
            {
                return;
            }

            if (namedTypeSymbol.HasAttributeInTypeAndBaseTyes(Definition.EnableClassAttribute))
            {
                return;
            }
            
            Diagnostic diagnostic = Diagnostic.Create(EntityClassDeclarationAnalyzerrRule.Rule, classDeclarationSyntax.Identifier.GetLocation(), namedTypeSymbol);
            context.ReportDiagnostic(diagnostic);
        }
    }
}


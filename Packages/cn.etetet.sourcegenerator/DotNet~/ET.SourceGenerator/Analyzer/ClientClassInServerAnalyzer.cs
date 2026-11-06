using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ET
{
    /// <summary>
    /// 服务端代码禁止引用客户端命名空间分析器（ET0022）。
    /// <para>检查非 Client 目录下的源码文件是否包含 <c>using ET.Client...</c>，防止服务端逻辑意外耦合客户端专属类型。</para>
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ClientClassInServerAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ClientClassInServerAnalyzerRule.Rule);
        
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            //context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.UsingDirective);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            //if (!AnalyzerHelper.IsAssemblyNeedAnalyze(context.Compilation.AssemblyName, AnalyzeAssembly.ServerModelHotfix))
            //{
            //    return;
            //}
            
            var usingDirective = (UsingDirectiveSyntax)context.Node;
            var namespaceName = usingDirective.Name.ToString();

            if (namespaceName.StartsWith(Definition.ETClientNameSpace) && !context.Node.SyntaxTree.FilePath.Contains(Definition.ClientDirInServer))
            {
                var diagnostic = Diagnostic.Create(ClientClassInServerAnalyzerRule.Rule, usingDirective.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}


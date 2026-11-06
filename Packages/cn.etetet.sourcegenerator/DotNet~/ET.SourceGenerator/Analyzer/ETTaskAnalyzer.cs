using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ET
{
    /// <summary>
    /// <c>ETTask</c> 异步调用与返回值规范分析器（ET0008 / ET0009 / ET0021）。
    /// <para>1. 在同步方法或同步 Lambda 中调用返回 <c>ETTask</c> 的方法时，强制要求链式调用 <c>.Coroutine()</c>（ET0008）。</para>
    /// <para>2. 在 <c>async</c> 异步方法或异步 Lambda 中调用返回 <c>ETTask</c> 的方法时，强制要求使用 <c>await</c> 等待或显式调用 <c>.Coroutine()</c>，防止意外丢弃异步任务导致异常被吞（ET0009）。</para>
    /// <para>3. 全局禁止声明 <c>async void</c> 方法、局部函数或 <c>async Action</c> Lambda 表达式，所有异步方法必须返回 <c>ETTask</c>（ET0021）。</para>
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ETTaskAnalyzer:DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
                ImmutableArray.Create(ETTaskInSyncMethodAnalyzerRule.Rule,ETTaskInAsyncMethodAnalyzerRule.Rule,AsyncMethodReturnTypeAnalyzerRule.Rule);
        
        public override void Initialize(AnalysisContext context)
        {
            if (!AnalyzerGlobalSetting.EnableAnalyzer)
            {
                return;
            }
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction((analysisContext =>
            {
                if (AnalyzerHelper.IsAssemblyNeedAnalyze(analysisContext.Compilation.AssemblyName, AnalyzeAssembly.AllModelHotfix))
                {
                    analysisContext.RegisterSemanticModelAction((this.AnalyzeSemanticModel));
                }
            } ));
            // 检测async void方法和Lambda
            context.RegisterSyntaxNodeAction(this.AnalyzeAsyncVoidMethod, SyntaxKind.MethodDeclaration, SyntaxKind.LocalFunctionStatement);
            context.RegisterSyntaxNodeAction(this.AnalyzeAsyncVoidLambda, SyntaxKind.ParenthesizedLambdaExpression, SyntaxKind.SimpleLambdaExpression);
        }
        
        private void AnalyzeSemanticModel(SemanticModelAnalysisContext analysisContext)
        {
            foreach (var memberAccessExpressionSyntax in analysisContext.SemanticModel.SyntaxTree.GetRoot().DescendantNodes<MemberAccessExpressionSyntax>())
            {
                AnalyzeMemberAccessExpression(analysisContext, memberAccessExpressionSyntax);
            }
        }

        private void AnalyzeMemberAccessExpression(SemanticModelAnalysisContext context, MemberAccessExpressionSyntax memberAccessExpressionSyntax)
        {
            // 获取方法调用Syntax 对应的methodSymbol
            if (!(memberAccessExpressionSyntax?.Parent is InvocationExpressionSyntax invocationExpressionSyntax) ||
                !(context.SemanticModel.GetSymbolInfo(invocationExpressionSyntax).Symbol is IMethodSymbol methodSymbol))
            {
                return;
            }

            //忽略void返回值函数
            if (methodSymbol.ReturnsVoid)
            {
                return;
            }
            
            if (!(methodSymbol.ReturnType is INamedTypeSymbol namedTypeSymbol))
            {
                return;
            }
            
            // 筛选出返回值为ETTask 和ETTask<T>的函数
            if (namedTypeSymbol.Name!=Definition.ETTask)
            {
                return;
            }
            
            // 获取ETTask函数调用处所在的函数体
            var containingMethodDeclarationSyntax = memberAccessExpressionSyntax?.GetNeareastAncestor<MethodDeclarationSyntax>();
            if (containingMethodDeclarationSyntax==null)
            {
                return;
            }
            
            IMethodSymbol? containingMethodSymbol = context.SemanticModel.GetDeclaredSymbol(containingMethodDeclarationSyntax);
            if (containingMethodSymbol==null)
            {
                return;
            }
            
            // ETTask函数在Lambda表达式中时
            var lambdaExpression = invocationExpressionSyntax.GetNeareastAncestor<LambdaExpressionSyntax>();
            if (lambdaExpression != null)
            {
                // 检查是否是async lambda
                bool isAsyncLambda = false;
                if (lambdaExpression is ParenthesizedLambdaExpressionSyntax parenthesizedLambda)
                {
                    isAsyncLambda = parenthesizedLambda.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword));
                }
                else if (lambdaExpression is SimpleLambdaExpressionSyntax simpleLambda)
                {
                    isAsyncLambda = simpleLambda.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword));
                }

                // 对于async lambda中的ETTask调用，检查是否有await
                if (isAsyncLambda)
                {
                    // 检查当前调用是否被await修饰
                    var lambdaAwaitExpression = invocationExpressionSyntax.GetNeareastAncestor<AwaitExpressionSyntax>();
                    if (lambdaAwaitExpression == null)
                    {
                        // 检查是否调用了.Coroutine()
                        if (!IsValidETTaskCall(invocationExpressionSyntax))
                        {
                            Diagnostic diagnostic = Diagnostic.Create(ETTaskInAsyncMethodAnalyzerRule.Rule, memberAccessExpressionSyntax?.Name.Identifier.GetLocation(),
                                memberAccessExpressionSyntax?.Name);
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
                else
                {
                    // 同步lambda中的ETTask调用，需要.Coroutine()
                    if (!IsValidETTaskCall(invocationExpressionSyntax))
                    {
                        Diagnostic diagnostic = Diagnostic.Create(ETTaskInSyncMethodAnalyzerRule.Rule, memberAccessExpressionSyntax?.Name.Identifier.GetLocation(),
                            memberAccessExpressionSyntax?.Name);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
                return;
            }
            
            
            // 检查普通方法中的ETTask调用
            // 先检查是否被await修饰
            var awaitExpression = invocationExpressionSyntax.GetNeareastAncestor<AwaitExpressionSyntax>();
            if (awaitExpression != null)
            {
                // 已经有await，是正确的用法
                return;
            }

            // 检查是否有正确的后缀调用
            if (IsValidETTaskCall(invocationExpressionSyntax))
            {
                // 已经有.Coroutine()是正确的用法
                return;
            }

            // 方法体内ETTask单独调用时
            if (invocationExpressionSyntax.Parent is ExpressionStatementSyntax)
            {
                if (containingMethodSymbol.IsAsync)
                {
                    Diagnostic diagnostic = Diagnostic.Create(ETTaskInAsyncMethodAnalyzerRule.Rule, memberAccessExpressionSyntax?.Name.Identifier.GetLocation(),
                        memberAccessExpressionSyntax?.Name);
                    context.ReportDiagnostic(diagnostic);
                }
                else
                {
                    Diagnostic diagnostic = Diagnostic.Create(ETTaskInSyncMethodAnalyzerRule.Rule, memberAccessExpressionSyntax?.Name.Identifier.GetLocation(),
                        memberAccessExpressionSyntax?.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        /// <summary>
        /// 检查ETTask调用是否已经正确处理（有.Coroutine()）
        /// </summary>
        private static bool IsValidETTaskCall(InvocationExpressionSyntax invocationExpression)
        {
            // 检查父级是否是成员访问表达式（如 .Coroutine()）
            if (invocationExpression.Parent is MemberAccessExpressionSyntax parentMemberAccess)
            {
                var memberName = parentMemberAccess.Name.Identifier.ValueText;
                if (memberName == "Coroutine")
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 检测async void方法
        /// </summary>
        private void AnalyzeAsyncVoidMethod(SyntaxNodeAnalysisContext context)
        {
            if (!AnalyzerGlobalSetting.EnableAnalyzer)
            {
                return;
            }

            if (!AnalyzerHelper.IsAssemblyNeedAnalyze(context.Compilation.AssemblyName, AnalyzeAssembly.All))
            {
                return;
            }

            IMethodSymbol? methodSymbol = null;

            if (context.Node is MethodDeclarationSyntax methodDeclarationSyntax)
            {
                methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclarationSyntax);
            }
            else if (context.Node is LocalFunctionStatementSyntax localFunctionStatementSyntax)
            {
                methodSymbol = context.SemanticModel.GetDeclaredSymbol(localFunctionStatementSyntax) as IMethodSymbol;
            }

            if (methodSymbol == null)
            {
                return;
            }

            if (methodSymbol.IsAsync && methodSymbol.ReturnsVoid)
            {
                Diagnostic diagnostic = Diagnostic.Create(AsyncMethodReturnTypeAnalyzerRule.Rule, context.Node.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }

        /// <summary>
        /// 检测async void Lambda表达式
        /// </summary>
        private void AnalyzeAsyncVoidLambda(SyntaxNodeAnalysisContext context)
        {
            if (!AnalyzerGlobalSetting.EnableAnalyzer)
            {
                return;
            }

            if (!AnalyzerHelper.IsAssemblyNeedAnalyze(context.Compilation.AssemblyName, AnalyzeAssembly.All))
            {
                return;
            }

            bool isAsync = false;
            if (context.Node is ParenthesizedLambdaExpressionSyntax parenthesizedLambda)
            {
                isAsync = parenthesizedLambda.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword));
            }
            else if (context.Node is SimpleLambdaExpressionSyntax simpleLambda)
            {
                isAsync = simpleLambda.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword));
            }

            if (isAsync)
            {
                // 检查lambda表达式的转换目标类型
                var typeInfo = context.SemanticModel.GetTypeInfo(context.Node);
                if (typeInfo.ConvertedType != null)
                {
                    // 检查是否是Action委托（对应async void）
                    if (typeInfo.ConvertedType.Name == "Action" ||
                        (typeInfo.ConvertedType.ContainingNamespace?.Name == "System" && typeInfo.ConvertedType.Name.StartsWith("Action")))
                    {
                        Diagnostic diagnostic = Diagnostic.Create(AsyncMethodReturnTypeAnalyzerRule.Rule, context.Node.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                        return;
                    }
                }

                // 检查lambda表达式的符号信息
                var symbolInfo = context.SemanticModel.GetSymbolInfo(context.Node);
                if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
                {
                    // 检查是否是async void（返回void）
                    if (methodSymbol.ReturnsVoid)
                    {
                        Diagnostic diagnostic = Diagnostic.Create(AsyncMethodReturnTypeAnalyzerRule.Rule, context.Node.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ET.Generator;

/// <summary>
/// 组件快捷获取扩展方法（<c>GetXxxComponent(this ParentEntity self)</c>）源码生成器。
/// <para>扫描标记了 <c>[ComponentOf(typeof(ParentEntity))]</c> 的组件类，为对应的父实体生成强类型的 <c>Get{ComponentName}()</c> 扩展方法。</para>
/// </summary>
[Generator(LanguageNames.CSharp)]
public class ETGetComponentGenerator : ISourceGenerator
{
    
    /// <summary>
    /// 初始化源码生成器（当前默认未注册通知）。
    /// </summary>
    public void Initialize(GeneratorInitializationContext context)
    {
        //context.RegisterForSyntaxNotifications(SyntaxContextReceiver.Create);
    }

    /// <summary>
    /// 根据按命名空间分组的 <c>[ComponentOf]</c> 元数据生成扩展方法源码文件。
    /// </summary>
    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not SyntaxContextReceiver receiver || receiver.ComponentOfDatas.Count==0 )
        {
            return;
        }

        foreach (var kv in receiver.ComponentOfDatas)
        {
            GenerateCSFiles(context, kv.Key,kv.Value);
        }

    }
    
    private void GenerateCSFiles(GeneratorExecutionContext context,string nameSpace, List<ComponentOfData> componentOfDatas)
    {
        StringBuilder contenSb = new StringBuilder();

        foreach (var componentOfData in componentOfDatas)
        {
            GenerateGetComponentCode(context, componentOfData, contenSb);
        }

        string? assemblyName = context.Compilation.AssemblyName?.Replace(".", "_");
        string code = $$"""
namespace {{nameSpace}}
{
    public static class {{assemblyName}}ETGetComponentExtension
    {
        {{contenSb}}
    }
}
""";
        context.AddSource($"ETGetComponentGenerator.{nameSpace}.g.cs", code);
    }

    private void GenerateGetComponentCode(GeneratorExecutionContext context,ComponentOfData componentOfData,StringBuilder contenSb)
    {
        string getComponentName = componentOfData.ComponentName;
        string parentEntityName = componentOfData.ParentEntityName;
        contenSb.AppendLine($$"""

        public static {{getComponentName}} Get{{getComponentName}} (this {{parentEntityName}} self)
        {
            return self.GetComponent<{{getComponentName}}>();
        }

""");
    }

    struct ComponentOfData
    {
        public string ComponentName;

        public string ParentEntityName;
    }

    class SyntaxContextReceiver: ISyntaxContextReceiver
    {
        internal static ISyntaxContextReceiver Create()
        {
            return new SyntaxContextReceiver();
        }
        
        public Dictionary<string, List<ComponentOfData>> ComponentOfDatas = new();

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            SyntaxNode node = context.Node;
            if (node is not ClassDeclarationSyntax classDeclarationSyntax)
            {
                return;
            }

            if (classDeclarationSyntax.AttributeLists.Count==0)
            {
                return;
            }
            
            var classTypeSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax) as INamedTypeSymbol;
            if (classTypeSymbol==null)
            {
                return;
            }
            // 筛选出含有 ComponentOf标签的
            var componentOfAttrData = classTypeSymbol.GetFirstAttribute(Definition.ComponentOfAttribute);
            if (componentOfAttrData==null)
            {
                return;
            }

            // 忽略无Type参数的
            if (componentOfAttrData.ConstructorArguments[0].Value is not INamedTypeSymbol parentEntityTypeSymbol)
            {
                return;
            }
            
            
            string? nameSpace = classTypeSymbol.GetNameSpace();
            if (nameSpace==null)
            {
                return;
            }

            if (!ComponentOfDatas.ContainsKey(nameSpace))
            {
                ComponentOfDatas.Add(nameSpace,new());
            }

            this.ComponentOfDatas[nameSpace].Add(new ComponentOfData()
            {
                ComponentName = classTypeSymbol.Name,
                ParentEntityName = parentEntityTypeSymbol.Name,
            });
        }
    }
}


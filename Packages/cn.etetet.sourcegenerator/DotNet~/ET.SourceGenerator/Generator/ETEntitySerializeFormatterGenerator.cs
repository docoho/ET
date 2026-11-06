#define DOTNET

using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ET;

/// <summary>
/// Entity 多态序列化格式化器（MemoryPackFormatter&lt;Entity&gt;）源码生成器。
/// <para>扫描所有标记了 <c>[MemoryPackable]</c> 的 <c>Entity</c> / <c>LSEntity</c> 子类，自动生成基于类名 LongHashCode 作为 Union Tag 的序列化与反序列化 switch 分发代码，并注入到 <c>EntitySerializeRegister.Register()</c> 分部方法中。</para>
/// </summary>
[Generator(LanguageNames.CSharp)]
public class ETEntitySerializeFormatterGenerator : ISourceGenerator
{
    /// <summary>
    /// 注册语法上下文接收器，用于在编译期收集所有可序列化的实体类。
    /// </summary>
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications((() => ETEntitySerializeFormatterSyntaxContextReceiver.Create()));
    }

    /// <summary>
    /// 根据收集到的实体类集合生成 <c>ETEntitySerializeFormatter</c> 和 <c>EntitySerializeRegister</c> 源码文件。
    /// </summary>
    public void Execute(GeneratorExecutionContext context)
    {
        
        if (context.SyntaxContextReceiver is not ETEntitySerializeFormatterSyntaxContextReceiver receiver || receiver.entities.Count==0)
        {
            return;
        }
        
        int count = receiver.entities.Count;
        string typeHashCodeMapDeclaration = GenerateTypeHashCodeMapDeclaration(receiver);
        string serializeContent = GenerateSerializeContent(receiver);
        string deserializeContent = GenerateDeserializeContent(receiver);
        
        string genericTypeParam = "<TBufferWriter>";
        string scopedCode = "";
        if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.DotNetAssembly", out string? _))
        {
            scopedCode = "scoped";
        }
        
        string code = $$"""
#nullable enable
#pragma warning disable CS0108 // hides inherited member
#pragma warning disable CS0162 // Unreachable code
#pragma warning disable CS0164 // This label has not been referenced
#pragma warning disable CS0219 // Variable assigned but never used
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8601 // Possible null reference assignment
#pragma warning disable CS8602
#pragma warning disable CS8604 // Possible null reference argument for parameter
#pragma warning disable CS8619
#pragma warning disable CS8620
#pragma warning disable CS8631 // The type cannot be used as type parameter in the generic type or method
#pragma warning disable CS8765 // Nullability of type of parameter
#pragma warning disable CS9074 // The 'scoped' modifier of parameter doesn't match overridden or implemented member
#pragma warning disable CA1050 // Declare types in namespaces.

using System;
using MemoryPack;

[global::MemoryPack.Internal.Preserve]
public class ETEntitySerializeFormatter : MemoryPackFormatter<global::{{Definition.EntityType}}>
{
    static readonly System.Collections.Generic.Dictionary<Type, long> __typeToTag = new({{count}})
    {
{{typeHashCodeMapDeclaration}}
    };
    
    [global::MemoryPack.Internal.Preserve]
    public override void Serialize{{genericTypeParam}}(ref MemoryPackWriter{{genericTypeParam}} writer,{{scopedCode}} ref global::{{Definition.EntityType}}? value)
    {

        if (value == null)
        {
            writer.WriteNullUnionHeader();
            return;
        }

        if (__typeToTag.TryGetValue(value.GetType(), out var tag))
        {
            writer.WriteValue<byte>(global::MemoryPack.MemoryPackCode.WideTag);
            writer.WriteValue<long>(tag);
            switch (tag)
            {
{{serializeContent}}               
                default:
                    break;
            }
        }
        else
        {
            MemoryPackSerializationException.ThrowNotFoundInUnionType(value.GetType(), typeof(global::{{Definition.EntityType}}));
        }
    }
    
    [global::MemoryPack.Internal.Preserve]
    public override void Deserialize(ref MemoryPackReader reader,{{scopedCode}} ref global::{{Definition.EntityType}}? value)
    {

        bool isNull = reader.ReadValue<byte>() == global::MemoryPack.MemoryPackCode.NullObject;
        if (isNull)
        {
            value = default;
            return;
        }
        
        var tag = reader.ReadValue<long>();

        switch (tag)
        {
{{deserializeContent}}
            default:
                //MemoryPackSerializationException.ThrowInvalidTag(tag, typeof(global::IForExternalUnion));
                break;
        }
    }
}
namespace ET
{
    public static partial class EntitySerializeRegister
    {
        static partial void Register()
        {
            if (!global::MemoryPack.MemoryPackFormatterProvider.IsRegistered<global::{{Definition.EntityType}}>())
            {
                global::MemoryPack.MemoryPackFormatterProvider.Register(new ETEntitySerializeFormatter());
            }
        }
    }
}
""";
        context.AddSource($"ETEntitySerializeFormatterGenerator.g.cs",code);
    }

    private string GenerateTypeHashCodeMapDeclaration(ETEntitySerializeFormatterSyntaxContextReceiver receiver)
    {
        StringBuilder sb = new StringBuilder();
        foreach (var entityName in receiver.entities)
        {
                sb.AppendLine($$"""        { typeof(global::{{entityName}}), {{SourceGenerator.SourceGenStringHashHelper.GetLongHashCode(entityName)}} },""");
        }
        return sb.ToString();
    }

    private string GenerateSerializeContent(ETEntitySerializeFormatterSyntaxContextReceiver receiver)
    {
        StringBuilder sb = new StringBuilder();
        foreach (var entityName in receiver.entities)
        {
                sb.AppendLine($$"""                case {{SourceGenerator.SourceGenStringHashHelper.GetLongHashCode(entityName)}}: writer.WritePackable(System.Runtime.CompilerServices.Unsafe.As<global::{{Definition.EntityType}}?, global::{{entityName}}>(ref value)); break;""");
        }
        return sb.ToString();
    }

    private string GenerateDeserializeContent(ETEntitySerializeFormatterSyntaxContextReceiver receiver)
    {
        StringBuilder sb = new StringBuilder();
        foreach (var entityName in receiver.entities)
        {
            sb.AppendLine($$"""
                    case {{SourceGenerator.SourceGenStringHashHelper.GetLongHashCode(entityName)}}:
                    if(value == null)
                    {
                        value = global::{{entityName}}.Fetch<global::{{entityName}}>();
                    }
                    if(value is global::{{entityName}})
                    {
                        reader.ReadPackable(ref System.Runtime.CompilerServices.Unsafe.As<global::{{Definition.EntityType}}?, global::{{entityName}}>(ref value));
                    }else{
                        value = (global::{{entityName}})reader.ReadPackable<global::{{entityName}}>();
                    }
                    break;
""");
        }
        return sb.ToString();
    }
    
    class ETEntitySerializeFormatterSyntaxContextReceiver : ISyntaxContextReceiver
    {
        internal static ISyntaxContextReceiver Create()
        {
            return new ETEntitySerializeFormatterSyntaxContextReceiver();
        }
        
        public HashSet<string> entities = new HashSet<string>();
        
        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (!AnalyzerHelper.IsAssemblyNeedAnalyze(context.SemanticModel.Compilation.AssemblyName,AnalyzeAssembly.AllLogicModel))
            {
                return;
            }

            if (context.Node is not ClassDeclarationSyntax classDeclarationSyntax)
            {
                return;
            }
            
            var classTypeSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
            if (classTypeSymbol==null)
            {
                return;
            }

            var baseType = classTypeSymbol.BaseType?.ToString();
            
            // 筛选出实体类
            if (baseType!= Definition.EntityType && baseType != Definition.LSEntityType)
            {
                return;
            }

            if (!AnalyzerHelper.HasAttribute((ITypeSymbol)classTypeSymbol, "MemoryPack.MemoryPackableAttribute"))
            {
                return;
            }
            
            entities.Add(classTypeSymbol.ToString());
        }
    }
}

using System;
using System.Collections.Generic;

namespace ET
{

    /// <summary>
    /// <see cref="ETSystemGenerator"/> 使用的代码生成字符串模板管理器。
    /// <para>维护 <c>[EntitySystem]</c>、<c>[LSEntitySystem]</c>、<c>[MessageHandler]</c>、<c>[ActorMessageHandler]</c>、<c>[ActorMessageLocationHandler]</c>、<c>[Event]</c> 等方法特性对应的包装类代码模板。</para>
    /// </summary>
    public class AttributeTemplate
    {
        private Dictionary<string, string> templates = new Dictionary<string, string>();

        /// <summary>
        /// 初始化所有受支持的方法级 Attribute 到对应 System/Handler/Event 包装类的代码模板映射。
        /// </summary>
        public AttributeTemplate()
        {
            this.templates.Add("EntitySystem", 
                $$"""
                        $attribute$
                        public class $argsTypesUnderLine$_$methodName$System: $methodName$System<$argsTypes$>
                        {   
                            protected override $returnType$ $methodName$($argsTypesVars$)
                            {
                                $return$$argsVars0$.$methodName$($argsVarsWithout0$);
                            }
                        }
                """);
            
            this.templates.Add("LSEntitySystem", 
                $$"""
                        $attribute$
                        public class $argsTypesUnderLine$_$methodName$System: $methodName$System<$argsTypes$>
                        {   
                            protected override void $methodName$($argsTypesVars$)
                            {
                                $argsVars0$.$methodName$($argsVarsWithout0$);
                            }
                        }
                """);
            
            this.templates.Add("MessageHandler", 
                $$"""
                        $attribute$
                	    public class $className$_$methodName$_Handler: MessageHandler<$argsTypesWithout0$>
                	    {
                	    	protected override async ETTask Run($argsTypesVars$)
                	    	{
                                await $className$.$methodName$($argsVars$);
                            }
                        }
                """);
            
            this.templates.Add("ActorMessageHandler", 
                $$"""
                        $attribute$
                	    public class $className$_$methodName$_Handler: ActorMessageHandler<$argsTypes$>
                	    {
                	    	protected override async ETTask Run($argsTypesVars$)
                	    	{
                                await $className$.$methodName$($argsVars$);
                            }
                        }
                """);
            
            this.templates.Add("ActorMessageLocationHandler", 
                $$"""
                        $attribute$
                	    public class $className$_$methodName$_Handler: ActorMessageLocationHandler<$argsTypes$>
                	    {
                	    	protected override async ETTask Run($argsTypesVars$)
                	    	{
                                await $className$.$methodName$($argsVars$);
                            }
                        }
                """);
            
            this.templates.Add("Event", 
                $$"""
                        $attribute$
                        public class $argsTypes2$_$methodName$: AEvent<$argsTypes$>
                        {
                            protected override async ETTask Run($argsTypesVars$)
                            {
                                await $className$.$methodName$($argsVars$);
                            }
                        }
                """);
        }

        public string Get(string attributeType)
        {
            if (!this.templates.TryGetValue(attributeType, out string template))
            {
                throw new Exception($"not config template: {attributeType}");
            }

            if (template == null)
            {
                throw new Exception($"not config template: {attributeType}");
            }

            return template;
        }

        public bool Contains(string attributeType)
        {
            return this.templates.ContainsKey(attributeType);
        }
    }
}
using System;
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 条件表达式变量注册中心单例，在启动时自动将所有 NumericType 枚举名注册为 BTNumericCompare，并扫描所有标记了 [ConditionVariable] 的自定义 BTCondition 节点类型。
    /// </summary>
    [CodeProcess]
    [AllowInstance]
    public class ConditionVariableRegistry : Singleton<ConditionVariableRegistry>, ISingletonAwake
    {
        private readonly Dictionary<string, Type> variableNodeTypes = new();
        private readonly Dictionary<string, NumericType> numericTypes = new();

        public void Awake()
        {
            this.RegisterNumericTypes();
            
            HashSet<Type> types = CodeTypes.Instance.GetTypes(typeof(ConditionVariableAttribute));
            foreach (Type type in types)
            {
                if (!typeof(BTCondition).IsAssignableFrom(type))
                {
                    throw new Exception($"condition variable node must inherit BTCondition: {type.FullName}");
                }

                object[] attrs = type.GetCustomAttributes(typeof(ConditionVariableAttribute), false);
                foreach (object attr in attrs)
                {
                    ConditionVariableAttribute conditionVariableAttribute = (ConditionVariableAttribute)attr;
                    if (string.IsNullOrWhiteSpace(conditionVariableAttribute.Variable))
                    {
                        throw new Exception($"condition variable is empty: {type.FullName}");
                    }

                    if (!this.variableNodeTypes.TryAdd(conditionVariableAttribute.Variable, type))
                    {
                        throw new Exception($"duplicate condition variable: {conditionVariableAttribute.Variable}");
                    }
                }
            }
        }

        private void RegisterNumericTypes()
        {
            foreach (NumericType numericType in Enum.GetValues(typeof(NumericType)))
            {
                string variable = numericType.ToString();
                this.numericTypes.Add(variable, numericType);
                this.variableNodeTypes.Add(variable, typeof(BTNumericCompare));
            }
        }

        public bool TryGetNodeType(string variable, out Type nodeType)
        {
            return this.variableNodeTypes.TryGetValue(variable, out nodeType);
        }

        public bool TryGetNumericType(string variable, out NumericType numericType)
        {
            return this.numericTypes.TryGetValue(variable, out numericType);
        }
    }
}

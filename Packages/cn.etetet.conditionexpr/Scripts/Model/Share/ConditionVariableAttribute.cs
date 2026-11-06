using System;

namespace ET
{
    /// <summary>
    /// 条件表达式自定义变量注册特性，贴在 BTCondition 子类上以声明其在表达式中对应的变量名（例如 [ConditionVariable("ItemCount")]）。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ConditionVariableAttribute : BaseAttribute
    {
        public string Variable { get; }

        public ConditionVariableAttribute(string variable)
        {
            this.Variable = variable;
        }
    }
}

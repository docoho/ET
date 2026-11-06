using System;

namespace ET
{
    /// <summary>
    /// 行为树（BehaviorTree）节点代码生成标记特性。
    /// 标注在静态方法上，指示生成器为该方法自动生成继承自 <see cref="NodeBaseType"/> 的行为树节点包装类与处理器。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class BTNodeGenAttribute: Attribute
    {
        /// <summary>生成的行为树节点所继承的基类类型。</summary>
        public Type NodeBaseType { get; }

        public BTNodeGenAttribute(Type nodeBaseType)
        {
            this.NodeBaseType = nodeBaseType ?? throw new ArgumentNullException(nameof(nodeBaseType));
        }
    }
}

namespace ET
{
    /// <summary>
    /// 数值属性比较条件节点处理器，从行为树环境（BTEnv）中获取目标 Unit 的指定 NumericType 属性值并与配置常数进行比较。
    /// </summary>
    public class BTNumericCompareHandler : ABTHandler<BTNumericCompare>
    {
        protected override int Run(BTNumericCompare node, BTEnv env)
        {
            Unit owner = env.GetEntity<Unit>(node.OwnerKey);
            NumericComponent numericComponent = owner.NumericComponent;
            long left = numericComponent.GetAsLong(node.NumericType);
            return ConditionCompareHelper.Compare(left, node.Op, node.Value) ? ErrorCode.ERR_Success : node.ErrorCode;
        }
    }
}

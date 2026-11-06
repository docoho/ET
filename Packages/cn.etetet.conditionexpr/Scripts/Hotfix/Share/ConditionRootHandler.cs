namespace ET
{
    /// <summary>
    /// 条件表达式行为树根节点处理器，驱动编译后的首个子表达式节点求值并返回错误码（0 表示满足条件）。
    /// </summary>
    public class ConditionRootHandler : ABTHandler<ConditionRoot>
    {
        protected override int Run(ConditionRoot node, BTEnv env)
        {
            if (node.Children == null || node.Children.Count == 0)
            {
                return ErrorCode.ERR_Success;
            }

            if (node.Children.Count > 1)
            {
                throw new System.Exception($"condition root child count error: {node.Children.Count}");
            }

            return BTDispatcher.Instance.Handle(node.Children[0], env);
        }
    }
}

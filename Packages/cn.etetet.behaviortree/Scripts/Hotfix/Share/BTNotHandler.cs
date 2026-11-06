namespace ET
{
    /// <summary>
    /// 逻辑取反装饰节点（BTNot）处理器：若子节点返回 0（成功）则返回配置的 ErrorCode，若子节点返回非 0（失败）则返回 0（成功）。
    /// </summary>
    public class BTNotHandler: ABTHandler<BTNot>
    {
        protected override int Run(BTNot node, BTEnv env)
        {
            if (node.Children == null || node.Children.Count == 0)
            {
                return node.ErrorCode;
            }

            int ret = BTDispatcher.Instance.Handle(node.Children[0], env);
            return ret == 0 ? node.ErrorCode : 0;
        }
    }
}

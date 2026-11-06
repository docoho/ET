namespace ET
{
    /// <summary>
    /// 选择组合节点（BTSelector，逻辑“或”）处理器：依次执行子节点，只要有一个子节点返回 0（成功）即刻返回 0；若全部失败则返回第一个失败子节点的错误码。
    /// </summary>
    public class BTSelectorHandler: ABTHandler<BTSelector>
    {
        protected override int Run(BTSelector node, BTEnv env)
        {
            int firstFailRet = -1;
            bool hasFailRet = false;
            foreach (BTNode subNode in node.Children)
            {
                int ret = BTDispatcher.Instance.Handle(subNode, env);
                if (ret == 0)
                {
                    return ret;
                }

                if (!hasFailRet)
                {
                    firstFailRet = ret;
                    hasFailRet = true;
                }
            }
            return firstFailRet;
        }
    }
}

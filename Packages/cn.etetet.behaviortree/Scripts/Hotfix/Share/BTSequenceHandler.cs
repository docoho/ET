namespace ET
{
    /// <summary>
    /// 顺序组合节点（BTSequence，逻辑“与”）处理器：依次执行子节点，若任一子节点返回非 0（失败）则立即中断并返回该错误码；全部成功则返回 0。
    /// </summary>
    public class BTSequenceHandler: ABTHandler<BTSequence>
    {
        protected override int Run(BTSequence node, BTEnv env)
        {
            foreach (BTNode subNode in node.Children)
            {
                int ret = BTDispatcher.Instance.Handle(subNode, env);
                if (ret != 0)
                {
                    return ret;
                }
            }
            return 0;
        }
    }
}
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 技能消耗行为树根节点（CostNode）处理器，按顺序执行所有消耗子节点。
    /// </summary>
    public class BTCostNodeHandler: ABTHandler<CostNode>
    {
        protected override int Run(CostNode node, BTEnv env)
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
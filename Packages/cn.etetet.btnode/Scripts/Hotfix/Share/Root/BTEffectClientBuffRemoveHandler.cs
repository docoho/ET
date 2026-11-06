using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 客户端 Buff 移除效果根节点（EffectClientBuffRemove）处理器，顺序驱动其子节点执行。
    /// </summary>
    public class BTEffectClientBuffRemoveHandler: ABTHandler<EffectClientBuffRemove>
    {
        protected override int Run(EffectClientBuffRemove node, BTEnv env)
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
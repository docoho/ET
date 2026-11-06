using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 客户端 Buff 受击效果根节点（EffectClientBuffHitted）处理器，顺序驱动其子节点执行。
    /// </summary>
    public class BTEffectClientBuffHittedHandler: ABTHandler<EffectClientBuffHitted>
    {
        protected override int Run(EffectClientBuffHitted node, BTEnv env)
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
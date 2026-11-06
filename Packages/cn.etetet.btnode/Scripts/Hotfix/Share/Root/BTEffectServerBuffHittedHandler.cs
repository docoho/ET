using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 服务端 Buff 受击效果根节点（EffectServerBuffHitted）处理器，顺序驱动其子节点执行。
    /// </summary>
    public class BTEffectServerBuffHittedHandler: ABTHandler<EffectServerBuffHitted>
    {
        protected override int Run(EffectServerBuffHitted node, BTEnv env)
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
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 服务端 Buff 周期性（Tick）效果根节点（EffectServerBuffTick）处理器，顺序驱动其子节点执行。
    /// </summary>
    public class BTEffectServerBuffTickHandler: ABTHandler<EffectServerBuffTick>
    {
        protected override int Run(EffectServerBuffTick node, BTEnv env)
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
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 服务端 Buff 添加效果根节点（EffectServerBuffAdd）处理器，顺序驱动其子节点执行。
    /// </summary>
    public class BTEffectServerBuffAddHandler: ABTHandler<EffectServerBuffAdd>
    {
        protected override int Run(EffectServerBuffAdd node, BTEnv env)
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
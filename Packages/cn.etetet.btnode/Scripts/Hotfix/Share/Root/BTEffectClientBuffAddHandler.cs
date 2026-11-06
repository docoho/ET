using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 客户端 Buff 添加效果根节点（EffectClientBuffAdd）处理器，顺序驱动其子节点执行。
    /// </summary>
    public class BTEffectClientBuffAddHandler: ABTHandler<EffectClientBuffAdd>
    {
        protected override int Run(EffectClientBuffAdd node, BTEnv env)
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
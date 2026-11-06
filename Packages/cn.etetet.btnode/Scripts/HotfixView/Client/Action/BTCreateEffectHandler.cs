using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 客户端在目标 Unit 的指定骨骼挂点（BindPoint）上创建一次性表现特效的节点处理器。
    /// </summary>
    public class BTCreateEffectHandler: ABTHandler<BTCreateEffect>
    {
        protected override int Run(BTCreateEffect node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            
            CreateEffectOnPosAsync(unit, node.EffectName, node.BindPoint, node.Duration).Coroutine();
            return 0;
        }
        
        private static async ETTask CreateEffectOnPosAsync(Unit unit, string effectName, BindPoint bindPoint, int duration)
        {
            EntityRef<Unit> unitRef = unit;
            GameObject go = await unit.Scene().GetComponent<ResourcesLoaderComponent>().LoadAssetAsync<GameObject>(effectName);
            unit = unitRef;
            EffectUnitHelper.Create(unit, bindPoint, go, true, duration);
        }
    }
}

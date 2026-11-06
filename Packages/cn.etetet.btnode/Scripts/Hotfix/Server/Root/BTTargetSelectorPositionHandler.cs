using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// 服务端地面点坐标目标选择器处理器，校验并截断最大施法距离后将目标点存入 SpellTargetComponent 和黑板。
    /// </summary>
    public class BTTargetSelectorPositionHandler : ABTHandler<TargetSelectorPosition>
    {
        protected override int Run(TargetSelectorPosition node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit unit = buff.GetCaster();
            TargetComponent targetComponent = unit.GetComponent<TargetComponent>();

            float3 pos = targetComponent.Position;
            
            if (math.distance(pos, unit.Position) > node.MaxDistance)
            {
                pos = unit.Position + math.normalize(pos - unit.Position) * node.MaxDistance;
            }
            targetComponent.Position = pos;
            
            buff.GetBuffData().GetComponent<SpellTargetComponent>().Position = pos;

            env.AddStruct(node.Pos, pos);
            return 0;
        }
    }
}
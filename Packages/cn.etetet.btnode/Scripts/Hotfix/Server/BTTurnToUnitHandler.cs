using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// 服务端朝向目标单位节点处理器，使施法者水平旋转朝向目标 Unit 的当前位置。
    /// </summary>
    public class BTTurnToUnitHandler: ABTHandler<BTTurnToUnit>
    {
        protected override int Run(BTTurnToUnit node, BTEnv env)
        {
            Unit caster = env.GetEntity<Unit>(node.Caster);
            Unit target = env.GetEntity<Unit>(node.Target);
            Buff buff = env.GetEntity<Buff>(node.Buff);

            float3 v = target.Position - caster.Position;
            v.y = 0;
            quaternion to = quaternion.LookRotation(v, math.up());
            caster.Turn(to, 100);
            
            return 0;
        }
    }
}
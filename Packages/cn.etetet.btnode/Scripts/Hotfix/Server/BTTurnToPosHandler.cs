using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// 服务端朝向坐标点节点处理器，使施法者水平旋转朝向指定的目标坐标点。
    /// </summary>
    public class BTTurnToPosHandler: ABTHandler<BTTurnToPos>
    {
        protected override int Run(BTTurnToPos node, BTEnv env)
        {
            Unit caster = env.GetEntity<Unit>(node.Caster);
            float3 pos = env.GetStruct<float3>(node.Pos);

            float3 v = pos - caster.Position;
            v.y = 0;
            quaternion to = quaternion.LookRotation(v, math.up());
            caster.Turn(to, 100);
            
            return 0;
        }
    }
}
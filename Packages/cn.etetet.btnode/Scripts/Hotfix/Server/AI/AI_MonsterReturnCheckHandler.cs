using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// 怪物脱战返程条件检查处理器：当怪物距离出生点超过 30 米时返回 0（触发返程）。
    /// </summary>
    public class AI_MonsterReturnCheckHandler: ABTHandler<AI_MonsterReturnCheck>
    {
        protected override int Run(AI_MonsterReturnCheck node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit unit = buff.GetOwner();
            NumericComponent numericComponent = unit.NumericComponent;
            float3 birthPos = new(
                numericComponent.GetAsFloat(NumericType.X),
                numericComponent.GetAsFloat(NumericType.Y),
                numericComponent.GetAsFloat(NumericType.Z));
            if (math.distance(unit.Position, birthPos) < 30f)
            {
                return 1;
            }
            return 0;
        }
    }
}

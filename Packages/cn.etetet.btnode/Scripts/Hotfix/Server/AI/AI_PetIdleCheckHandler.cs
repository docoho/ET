using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// 宠物待机条件检查处理器：当宠物无攻击目标且距离主人在 3 米以内时返回 0（进入待机漫步）。
    /// </summary>
    public class AI_PetIdleCheckHandler: ABTHandler<AI_PetIdleCheck>
    {
        protected override int Run(AI_PetIdleCheck node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit unit = buff.GetOwner();
            TargetComponent targetComponent = unit.GetComponent<TargetComponent>();
            Unit target = targetComponent.Unit;
            if (target != null)
            {
                return 1;
            }

            Unit owner = PetHelper.GetOwner(unit);
            if (math.distance(unit.Position, owner.Position) > 3f)
            {
                return 1;
            }
            return 0;
        }
    }
}

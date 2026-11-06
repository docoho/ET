using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// 宠物追击条件检查处理器：当宠物存在非主人的有效攻击目标时返回 0（触发追击）。
    /// </summary>
    public class AI_PetZhuiJiCheckHandler: ABTHandler<AI_PetZhuiJiCheck>
    {
        protected override int Run(AI_PetZhuiJiCheck node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit unit = buff.GetOwner();
            TargetComponent targetComponent = unit.GetComponent<TargetComponent>();
            Unit target = targetComponent.Unit;
            if (target == null)
            {
                return 1;
            }

            // 自己阵营的不攻击
            if (PetHelper.GetOwner(unit).Id == target.Id)
            {
                return 1;
            }
            return 0;
        }
    }
}

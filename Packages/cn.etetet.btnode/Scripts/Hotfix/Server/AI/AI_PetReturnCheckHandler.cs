using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// 宠物强制召回条件检查处理器：当宠物与主人距离超过 30 米时返回 0（触发脱战召回）。
    /// </summary>
    public class AI_PetReturnCheckHandler: ABTHandler<AI_PetReturnCheck>
    {
        protected override int Run(AI_PetReturnCheck node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit unit = buff.GetOwner();
            Unit owner = PetHelper.GetOwner(unit);
            if (math.distance(unit.Position, owner.Position) < 30f)
            {
                return 1;
            }
            return 0;
        }
    }
}

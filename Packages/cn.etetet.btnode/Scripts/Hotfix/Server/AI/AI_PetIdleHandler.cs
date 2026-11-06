using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// 宠物待机异步协程处理器：在主人身边 1~2 米范围内随机走动并等待。
    /// </summary>
    public class AI_PetIdleHandler: ABTCoroutineHandler<AI_PetIdle>
    {
        protected override async ETTask RunAsync(AI_PetIdle node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit unit = buff.GetOwner();
            TimerComponent timerComponent = unit.Root().TimerComponent;
            EntityRef<Unit> unitRef = unit;
            Unit owner = PetHelper.GetOwner(unit);
            EntityRef<Unit> ownerRef = owner;
            PathfindingComponent pathfindingComponent = unit.GetComponent<PathfindingComponent>();
            EntityRef<PathfindingComponent> pathfindingComponentRef = pathfindingComponent;
            ETCancellationToken cancellationToken = await ETTask.GetContextAsync<ETCancellationToken>();
            
            // 暂时写死
            BuffHelper.RemoveBuffByConfigId(unit, 200111, BuffFlags.AIRemove);
            
            while (true)
            {
                pathfindingComponent = pathfindingComponentRef;
                owner = ownerRef;
                float3 pos = pathfindingComponent.FindRandomPointWithRaduis(owner.Position, 1, 2);
                unit = unitRef;
                await unit.FindPathMoveToAsync(pos);
                if (cancellationToken.IsCancel())
                {
                    return;
                }
                
                await timerComponent.WaitAsync(RandomGenerator.RandomNumber(10000, 20000));
                if (cancellationToken.IsCancel())
                {
                    return;
                }
            }
        }
    }
}
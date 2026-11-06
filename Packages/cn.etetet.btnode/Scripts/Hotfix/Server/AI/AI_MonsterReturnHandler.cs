using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// 怪物脱战返程异步协程处理器：清空仇恨列表并寻路移动回出生点坐标。
    /// </summary>
    public class AI_MonsterReturnHandler: ABTCoroutineHandler<AI_MonsterReturn>
    {
        protected override async ETTask RunAsync(AI_MonsterReturn node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit unit = buff.GetOwner();
            NumericComponent numericComponent = unit.NumericComponent;

            float3 birthPos = new(
                numericComponent.GetAsFloat(NumericType.X), 
                numericComponent.GetAsFloat(NumericType.Y),
                numericComponent.GetAsFloat(NumericType.Z));
            
            ThreatComponent threatComponent = unit.GetComponent<ThreatComponent>();

            threatComponent.ClearThreat();

            TimerComponent timerComponent = unit.Root().TimerComponent;
            
            ETCancellationToken cancellationToken = await ETTask.GetContextAsync<ETCancellationToken>();
            
            await unit.FindPathMoveToAsync(birthPos);
            if (cancellationToken.IsCancel())
            {
                return;
            }
            
            while (true)
            {
                await timerComponent.WaitAsync(1000);
                if (cancellationToken.IsCancel())
                {
                    return;
                }
            }
        }
    }
}
using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// 怪物追击与攻击异步协程处理器：每 200ms 锁定最高仇恨目标，根据技能施法距离自动追击或后退，并在进入射程后自动释放技能。
    /// </summary>
    public class AI_MonsterZhuiJiHandler: ABTCoroutineHandler<AI_MonsterZhuiJi>
    {
        protected override async ETTask RunAsync(AI_MonsterZhuiJi node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit unit = buff.GetOwner();
            EntityRef<Unit> unitRef = unit;
            ThreatComponent threatComponent = unit.GetComponent<ThreatComponent>();
            EntityRef<ThreatComponent> threatComponentRef = threatComponent;

            TimerComponent timerComponent = unit.Root().TimerComponent;
            
            float unitRadius = unit.NumericComponent.GetAsFloat(NumericType.Radius);
            
            ETCancellationToken cancellationToken = await ETTask.GetContextAsync<ETCancellationToken>();

            SpellHelper.Cast(unit, 100110);
            
            while (true)
            {
                await timerComponent.WaitAsync(200);
                if (cancellationToken.IsCancel())
                {
                    return;
                }
                
                // 找到仇恨最大的作为自己的目标
                threatComponent = threatComponentRef;
                ThreatInfo threatInfo = threatComponent.GetMaxThreat();
                Unit target = threatInfo.Unit;
                if (target == null)
                {
                    continue;
                }

                unit = unitRef;
                unit.GetComponent<TargetComponent>().Unit = target;
                // 选择技能，移动到技能攻击范围
                int spellId = 100100;
                SpellConfig spellConfig = unit.Fiber().GetSingleton<SpellConfigCategory>().Get(spellId);
                float distance = math.distance(unit.Position, target.Position);
                float targetRadius = target.NumericComponent.GetAsFloat(NumericType.Radius);
                float d1 = distance - targetRadius - unitRadius;
                
                if (d1 > spellConfig.TargetSelector.MaxDistance / 1000f)
                {
                    // 走过去
                    unit.FindPathMoveToAsync(target.Position).Coroutine(cancellationToken);
                    continue;
                }
                
                if (d1 < spellConfig.TargetSelector.MinDistance / 1000f)
                {
                    // 反向走
                    unit.FindPathMoveToAsync(unit.Position - target.Position + unit.Position).Coroutine(cancellationToken);
                    continue;
                }

                unit.Stop(0);

                // 同一个技能还未结束
                Buff current = unit.GetComponent<SpellComponent>().Current;
                if (current != null && spellConfig.Id == current.ConfigId)
                {
                    continue;
                }
                // 施放技能
                SpellHelper.Cast(unit, spellId);
            }
        }
    }
}

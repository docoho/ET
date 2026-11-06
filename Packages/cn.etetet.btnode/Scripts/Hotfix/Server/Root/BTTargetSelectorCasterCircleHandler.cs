using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// 服务端以施法者自身为圆心的圆形范围目标选择器处理器，遍历 AOI 可见单位并通过子节点过滤条件筛选出目标列表。
    /// </summary>
    public class BTTargetSelectorCasterCircleHandler: ABTHandler<TargetSelectorCasterCircle>
    {
        protected override int Run(TargetSelectorCasterCircle node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            SpellTargetComponent spellTargetComponent = buff.GetBuffData().GetComponent<SpellTargetComponent>();

            Unit caster = buff.GetCaster();
            spellTargetComponent.Position = caster.Position;

            Dictionary<long, EntityRef<AOIEntity>> seeUnits = caster.GetComponent<AOIEntity>().GetSeeUnits();

            float3 pos = caster.Position;

            foreach ((long _, AOIEntity aoiEntity) in seeUnits)
            {
                Unit unit = aoiEntity.Unit;
                
                if (!unit.UnitType.IsSame(node.UnitType))
                {
                    continue;
                }
                
                if (math.distance(pos, unit.Position) > node.Radius)
                {
                    continue;
                }

                // 执行过滤条件判断
                if (node.Children.Count > 0)
                {
                    env.AddEntity(node.Unit, unit);
                    bool filter = false;
                    foreach (BTNode child in node.Children)
                    {
                        int ret = BTDispatcher.Instance.Handle(child, env);
                        if (ret == 0)
                        {
                            continue;
                        }

                        filter = true;
                        break;
                    }

                    if (filter)
                    {
                        continue;
                    }
                }

                spellTargetComponent.Units.Add(aoiEntity.Unit.Id);
            }
            return 0;
        }
    }
}
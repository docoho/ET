using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 遍历目标单位列表装饰节点处理器，依次从黑板的 Units 集合中取出每个 Unit 实体写入黑板并驱动所有子节点执行。
    /// </summary>
    public class BTForeachUnitHandler: ABTHandler<BTForeachUnit>
    {
        protected override int Run(BTForeachUnit node, BTEnv env)
        {
            Scene scene = env.Scene;
            List<long> units = env.GetCollection<List<long>>(node.Units);
            UnitComponent unitComponent = scene.GetComponent<UnitComponent>();
            foreach (long unitId in units)
            {
                Unit unit = unitComponent.Get(unitId);
                if (unit == null)
                {
                    continue;
                }
                
                env.AddEntity(node.Unit, unit);
                foreach (BTNode subNode in node.Children)
                {
                    BTDispatcher.Instance.Handle(subNode, env);
                }
            }
            return 0;
        }
    }
}
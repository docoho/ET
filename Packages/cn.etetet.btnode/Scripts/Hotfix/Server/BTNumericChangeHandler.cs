namespace ET.Server
{
    /// <summary>
    /// 服务端属性增减节点处理器：修改目标 Unit 的指定数值属性，若修改的是二级子属性（>= NumericComponentSystem.Max），则记录到 BuffChangeNumericRecordComponent 中以便 Buff 移除时自动回滚。
    /// </summary>
    public class BTNumericChangeHandler: ABTHandler<BTNumericChange>
    {
        protected override int Run(BTNumericChange node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            NumericComponent numericComponent = unit.NumericComponent;
            long value = numericComponent.GetAsLong(node.NumericType);
            numericComponent.Set(node.NumericType, value + node.Value);
            
            
            if ((int)node.NumericType < NumericComponentSystem.Max)
            {
                return 0;
            }
            
            
            Buff buff = env.GetEntity<Buff>(node.Buff);
            // buff删除的时候会还原数值
            BuffChangeNumericRecordComponent buffChangeNumericRecordComponent = 
                    buff.GetComponent<BuffChangeNumericRecordComponent>() ??
                    buff.AddComponent<BuffChangeNumericRecordComponent>();
            buffChangeNumericRecordComponent.Add(node.NumericType, node.Value);
            
            return 0;
        }
    }
}
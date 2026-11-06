namespace ET
{
    /// <summary>
    /// 技能消耗数值节点处理器：当黑板中的 Check 为 true 时仅检查施法者对应属性（如 MP/HP）是否足够并返回相应错误码，当 Check 为 false 时实际扣除数值。
    /// </summary>
    public class BTCostNumericHandler: ABTHandler<BTCostNumeric>
    {
        protected override int Run(BTCostNumeric node, BTEnv env)
        {
            Unit caster = env.GetEntity<Unit>(node.Caster);
            bool check = env.GetStruct<bool>(node.Check);

            NumericComponent numericComponent = caster.NumericComponent;
            long a = numericComponent.Get(node.NumericType);
            
            if (check)
            {
                if (a >= node.Value)
                {
                    return 0;
                }

                switch (node.NumericType)
                {
                    case NumericType.MP:
                        return TextConstDefine.SpellCast_MPNotEnought;
                    case NumericType.HP:
                        return TextConstDefine.SpellCast_HPNotEnought;
                }
            }
            else
            {
                numericComponent.Set(node.NumericType, a - node.Value);
            }

            return 0;
        }
    }
}
namespace ET.Server
{
    /// <summary>
    /// 服务端选择施法者自身作为唯一技能目标的目标选择器处理器。
    /// </summary>
    public class BTTargetSelectorCasterHandler: ABTHandler<TargetSelectorCaster>
    {
        protected override int Run(TargetSelectorCaster node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);

            Unit unit = buff.GetCaster();
            
            buff.GetBuffData().GetComponent<SpellTargetComponent>().Units.Add(unit.Id);
            
            return 0;
        }
    }
}
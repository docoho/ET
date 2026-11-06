namespace ET.Server
{
    /// <summary>
    /// 服务端造成伤害节点处理器，调用 DamageHelper.Damage 执行伤害结算与仇恨/受击触发。
    /// </summary>
    public class BTDamageHandler: ABTHandler<BTDamage>
    {
        protected override int Run(BTDamage node, BTEnv env)
        {
            Unit caster = env.GetEntity<Unit>(node.Caster);
            Unit target = env.GetEntity<Unit>(node.Target);
            Buff buff = env.GetEntity<Buff>(node.Buff);
            
            DamageHelper.Damage(caster, target, buff, node.Value);
            return 0;
        }
    }
}
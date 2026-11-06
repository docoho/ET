namespace ET.Server
{
    /// <summary>
    /// 服务端派生/触发子技能节点处理器，在当前 Buff 上下文下调用 SpellHelper.Cast 释放指定子技能。
    /// </summary>
    public class BTCreateSpellHandler: ABTHandler<BTCreateSpell>
    {
        protected override int Run(BTCreateSpell node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            env.TryGetEntity(node.Buff, out Buff buff);
            return SpellHelper.Cast(unit, node.SpellConfigId, buff);
        }
    }
}
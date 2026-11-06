namespace ET
{
    /// <summary>
    /// 从当前 Buff（或其父技能 Buff）的 SpellTargetComponent 中获取所有选中的目标 Unit ID 列表并写入黑板的节点处理器。
    /// </summary>
    public class BTGetSpellTargetUnitsHandler: ABTHandler<BTGetSpellTargetUnits>
    {
        protected override int Run(BTGetSpellTargetUnits node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            SpellTargetComponent spellTargetComponent = buff.GetBuffData().GetComponent<SpellTargetComponent>();
            env.AddCollection(node.Units, spellTargetComponent.Units);
            return 0;
        }
    }
}
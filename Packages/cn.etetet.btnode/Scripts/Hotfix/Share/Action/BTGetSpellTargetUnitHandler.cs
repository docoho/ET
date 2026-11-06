namespace ET
{
    /// <summary>
    /// 从当前 Buff（或其父技能 Buff）的 SpellTargetComponent 中获取首个选中的目标 Unit 并写入黑板的节点处理器。
    /// </summary>
    public class BTGetSpellTargetUnitHandler: ABTHandler<BTGetSpellTargetUnit>
    {
        protected override int Run(BTGetSpellTargetUnit node, BTEnv env)
        {
            Scene scene = env.Scene;
            Buff buff = env.GetEntity<Buff>(node.Buff);
            SpellTargetComponent spellTargetComponent = buff.GetBuffData().GetComponent<SpellTargetComponent>();
            if (spellTargetComponent.Units.Count == 0)
            {
                return TextConstDefine.SpellCast_NotSelectTarget;
            }
            Unit target = scene.GetComponent<UnitComponent>().Get(spellTargetComponent.Units[0]);
            env.AddEntity(node.Unit, target);
            return 0;
        }
    }
}
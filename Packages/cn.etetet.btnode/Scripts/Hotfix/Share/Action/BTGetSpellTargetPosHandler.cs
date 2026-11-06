namespace ET
{
    /// <summary>
    /// 从当前 Buff（或其父技能 Buff）的 SpellTargetComponent 中读取目标地面坐标并写入黑板的节点处理器。
    /// </summary>
    public class BTGetSpellTargetPosHandler: ABTHandler<BTGetSpellTargetPos>
    {
        protected override int Run(BTGetSpellTargetPos node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            SpellTargetComponent spellTargetComponent = buff.GetBuffData().GetComponent<SpellTargetComponent>();
            env.AddStruct(node.Pos, spellTargetComponent.Position);
            return 0;
        }
    }
}
namespace ET
{
    /// <summary>
    /// 获取 Buff 所属的 Unit 并写入黑板的节点处理器。
    /// </summary>
    public class BTGetBuffCasterHandler: ABTHandler<BTGetBuffCaster>
    {
        protected override int Run(BTGetBuffCaster node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            env.AddEntity(node.Unit, buff.Parent.GetParent<Unit>());
            return 0;
        }
    }
}
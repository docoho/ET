namespace ET.Server
{
    /// <summary>
    /// 获取当前 Buff 的宿主（Owner）Unit 并写入行为树黑板的节点处理器。
    /// </summary>
    public class BTGetBuffOwnerHandler: ABTHandler<BTGetBuffOwner>
    {
        protected override int Run(BTGetBuffOwner node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            env.AddEntity(node.Unit, buff.GetOwner());
            return 0;
        }
    }
}
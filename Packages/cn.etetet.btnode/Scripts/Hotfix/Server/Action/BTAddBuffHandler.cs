namespace ET.Server
{
    /// <summary>
    /// 服务端添加 Buff 节点处理器，在目标 Unit 上创建并初始化指定 ConfigId 的子 Buff。
    /// </summary>
    public class BTAddBuffHandler: ABTHandler<BTAddBuff>
    {
        protected override int Run(BTAddBuff node, BTEnv env)
        {
            Unit caster = env.GetEntity<Unit>(node.Caster);
            Unit target = env.GetEntity<Unit>(node.Target);
            Buff parent = env.GetEntity<Buff>(node.Buff);
            BuffHelper.CreateBuff(target, caster.Id, IdGenerater.Instance.GenerateId(), node.ConfigId, parent);
            return 0;
        }
    }
}
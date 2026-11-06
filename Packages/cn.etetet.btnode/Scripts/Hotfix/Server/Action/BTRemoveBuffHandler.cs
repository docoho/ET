namespace ET.Server
{
    /// <summary>
    /// 服务端按配置 ID 移除目标 Unit 身上指定 Buff 的节点处理器。
    /// </summary>
    public class BTRemoveBuffHandler: ABTHandler<BTRemoveBuff>
    {
        protected override int Run(BTRemoveBuff node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            BuffHelper.RemoveBuffByConfigId(unit, node.ConfigId, node.RemoveType);
            return 0;
        }
    }
}
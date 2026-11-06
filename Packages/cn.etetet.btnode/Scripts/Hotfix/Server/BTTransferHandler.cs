namespace ET.Server
{
    /// <summary>
    /// 服务端跨地图传送节点处理器，根据配置的 TransferId 查询传送规则并触发目标 Unit 的异步跨服/跨场景传送。
    /// </summary>
    public class BTTransferHandler: ABTHandler<BTTransfer>
    {
        protected override int Run(BTTransfer node, BTEnv env)
        {
            Unit target = env.GetEntity<Unit>(node.Target);
            
            // 获取传送规则
            MapTransferRuleConfig mapTransferRuleConfig = target.Fiber().GetSingleton<MapTransferRuleConfigCategory>().Get(node.TransferId);

            TransferHelper.TransferLock(target, mapTransferRuleConfig.ToMap, 0, true).Coroutine();
            return 0;
        }
    }
}

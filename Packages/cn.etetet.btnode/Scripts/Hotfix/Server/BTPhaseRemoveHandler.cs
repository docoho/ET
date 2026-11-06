namespace ET.Server
{
    /// <summary>
    /// 服务端移除位面（Phase）节点处理器，将目标 Unit 从指定的位面中移出。
    /// </summary>
    public class BTPhaseRemoveHandler: ABTHandler<BTPhaseRemove>
    {
        protected override int Run(BTPhaseRemove node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            unit.RemovePhase(node.phaseType);
            return 0;
        }
    }
}
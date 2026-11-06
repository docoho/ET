namespace ET.Server
{
    /// <summary>
    /// 服务端添加位面（Phase）节点处理器，将目标 Unit 加入指定的位面。
    /// </summary>
    public class BTPhaseAddHandler: ABTHandler<BTPhaseAdd>
    {
        protected override int Run(BTPhaseAdd node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            unit.AddPhase(node.phaseType);
            return 0;
        }
    }
}
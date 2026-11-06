namespace ET
{
    /// <summary>
    /// 获取指定 Unit 身上 TargetComponent 记录的目标坐标点并写入黑板的节点处理器。
    /// </summary>
    public class BTGetUnitTargetPosHandler: ABTHandler<BTGetUnitTargetPos>
    {
        protected override int Run(BTGetUnitTargetPos node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            TargetComponent targetComponent = unit.GetComponent<TargetComponent>();
            env.AddStruct(node.Pos, targetComponent.Position);
            return 0;
        }
    }
}
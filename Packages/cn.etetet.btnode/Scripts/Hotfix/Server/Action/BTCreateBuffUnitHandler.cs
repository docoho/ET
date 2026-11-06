namespace ET.Server
{
    /// <summary>
    /// 服务端创建 Buff 附属场景单位节点处理器，创建出的 Unit 会记录在 BuffUnitComponent 中，随 Buff 销毁而自动移除。
    /// </summary>
    public class BTCreateBuffUnitHandler: ABTHandler<BTCreateBuffUnit>
    {
        protected override int Run(BTCreateBuffUnit node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);

            Unit unit = UnitFactory.Create(env.Scene, IdGenerater.Instance.GenerateId(), node.UnitConfigId);
            buff.AddComponent<BuffUnitComponent>().UnitIds.Add(unit.Id);
            env.AddEntity(node.Unit, unit);
            return 0;
        }
    }
}
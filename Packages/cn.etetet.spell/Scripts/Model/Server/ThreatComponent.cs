namespace ET.Server
{
    /// <summary>
    /// 服务端仇恨管理组件，挂载在怪物或 NPC 的 Unit 上，其子实体（Children）存储各个目标的 ThreatInfo。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class ThreatComponent: Entity, IAwake
    {
    }
}

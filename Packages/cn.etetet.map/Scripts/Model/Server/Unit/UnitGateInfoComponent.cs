namespace ET.Server
{
    /// <summary>
    /// 玩家单位关联的 Gate 网关信息组件（支持随 Unit 跨场景/跨进程传送，用于从 Map 向玩家客户端推送消息）。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class UnitGateInfoComponent: Entity, IAwake, ITransfer
    {
        /// <summary>
        /// 玩家在 Gate 服上的代理实体 ActorId。
        /// </summary>
        public ActorId ActorId;
    }
}
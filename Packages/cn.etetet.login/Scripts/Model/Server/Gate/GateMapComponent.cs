namespace ET.Server
{
    /// <summary>
    /// 网关玩家临时地图组件：用于在玩家首次进图时持有临时创建的 GateMap 纤程引用。
    /// </summary>
    [ComponentOf(typeof(Player))]
    public class GateMapComponent: Entity, IAwake, IDestroy
    {
        /// <summary>临时 GateMap 场景纤程实例</summary>
        public Fiber Fiber { get; set; }
    }
}
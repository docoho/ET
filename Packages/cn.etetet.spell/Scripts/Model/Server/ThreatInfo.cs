namespace ET.Server
{
    /// <summary>
    /// 单条仇恨记录子实体，挂载在 ThreatComponent 下，记录某个目标 Unit 及其累积的仇恨值。
    /// </summary>
    [ChildOf(typeof(ThreatComponent))]
    public class ThreatInfo: Entity, IAwake
    {
        /// <summary>
        /// 产生仇恨的目标 Unit 引用。
        /// </summary>
        public EntityRef<Unit> Unit { get; set; }
        /// <summary>
        /// 当前累积的仇恨数值。
        /// </summary>
        public int Threat { get; set; }
    }
}

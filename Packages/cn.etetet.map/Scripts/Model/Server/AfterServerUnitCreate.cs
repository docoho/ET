namespace ET.Server
{
    /// <summary>
    /// 服务端地图单位创建完成事件。
    /// </summary>
    public struct AfterServerUnitCreate
    {
        /// <summary>
        /// 新创建的单位实体引用。
        /// </summary>
        public EntityRef<Unit> Unit;
    }
}

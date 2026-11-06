using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// AOI 空间网格单元实体（以 <c>CreateCellId(x, y)</c> 为实体 Id，记录位于本格内的单位以及订阅本格进入/离开事件的观察者）。
    /// </summary>
    [ChildOf(typeof(AOIManagerComponent))]
    public class Cell: Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 当前中心点处于该网格内的所有 <see cref="AOIEntity"/> 字典。
        /// </summary>
        // 处在这个cell的单位
        public Dictionary<long, EntityRef<AOIEntity>> AOIUnits = new Dictionary<long, EntityRef<AOIEntity>>();

        /// <summary>
        /// 订阅了该网格“进入视野”事件的所有 <see cref="AOIEntity"/> 字典。
        /// </summary>
        // 订阅了这个Cell的进入事件
        public Dictionary<long, EntityRef<AOIEntity>> SubsEnterEntities = new Dictionary<long, EntityRef<AOIEntity>>();

        /// <summary>
        /// 订阅了该网格“离开视野”事件的所有 <see cref="AOIEntity"/> 字典。
        /// </summary>
        // 订阅了这个Cell的退出事件
        public Dictionary<long, EntityRef<AOIEntity>> SubsLeaveEntities = new Dictionary<long, EntityRef<AOIEntity>>();
    }
}
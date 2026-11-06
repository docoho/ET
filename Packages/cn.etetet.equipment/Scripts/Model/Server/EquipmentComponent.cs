using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 服务端装备管理组件（挂载在 <see cref="Unit"/> 上，管理角色已穿戴的装备 <see cref="Item"/> 实体）
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class EquipmentComponent: Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 装备槽位字典（槽位类型 -> 穿戴的Item的EntityRef）
        /// </summary>
        public Dictionary<EquipmentSlotType, EntityRef<Item>> EquippedItems = new();
    }
}
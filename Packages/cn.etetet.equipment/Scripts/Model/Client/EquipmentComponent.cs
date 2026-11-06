using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 客户端装备管理组件（挂载在客户端 <see cref="Scene"/> 上，缓存当前角色已穿戴的各部位装备引用）
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class EquipmentComponent: Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 装备槽位字典（槽位类型 -> 装备EntityRef）
        /// </summary>
        public Dictionary<EquipmentSlotType, EntityRef<Item>> EquippedItems = new();
    }
}
namespace ET.Client
{
    /// <summary>
    /// 客户端装备物品扩展组件（挂载在客户端 <see cref="Item"/> 上，标记该物品为装备并记录强化等专有属性）
    /// <para><see cref="Item.SlotIndex"/> 用于表示装备槽位类型，穿戴状态通过 <see cref="EquipmentComponent"/> 是否包含该 Item 来判断</para>
    /// </summary>
    [ComponentOf(typeof(Item))]
    public class EquipmentItemComponent: Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 装备强化等级
        /// </summary>
        public int EnhanceLevel;
    }
}

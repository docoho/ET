namespace ET.Server
{
    /// <summary>
    /// 服务端装备管理组件系统，负责管理角色已穿戴装备的生命周期、槽位挂载与卸下操作
    /// </summary>
    [EntitySystemOf(typeof(EquipmentComponent))]
    public static partial class EquipmentComponentSystem
    {
        #region 生命周期方法

        /// <summary>
        /// 组件初始化时清空已装备物品字典
        /// </summary>
        /// <param name="self">装备管理组件实例</param>
        [EntitySystem]
        private static void Awake(this EquipmentComponent self)
        {
            self.EquippedItems.Clear();
        }

        /// <summary>
        /// 组件销毁时清空已装备物品字典
        /// </summary>
        /// <param name="self">装备管理组件实例</param>
        [EntitySystem]
        private static void Destroy(this EquipmentComponent self)
        {
            self.EquippedItems.Clear();
        }

        #endregion

        #region 业务方法

        /// <summary>
        /// 穿戴装备（将 Item 实体从背包转移为装备组件的子实体并记录到对应装备槽位）
        /// </summary>
        /// <param name="self">装备管理组件实例</param>
        /// <param name="item">要穿戴的物品实体</param>
        /// <param name="slotType">目标装备槽位类型</param>
        public static void EquipItem(this EquipmentComponent self, Item item, EquipmentSlotType slotType)
        {
            if (item == null)
            {
                throw new System.Exception("Item cannot be null");
            }

            // 将物品实体挂载为装备管理组件的子节点
            self.AddChild(item);

            // 检查Item是否有装备组件
            item.AddComponent<EquipmentItemComponent>();


            // 如果该槽位已有装备，先卸下
            if (self.EquippedItems.ContainsKey(slotType))
            {
                Item oldItem = self.EquippedItems[slotType];
                if (oldItem != null)
                {
                    // 卸下旧装备，设置SlotIndex为-1表示未装备
                    oldItem.SlotIndex = -1;
                }
            }

            // 穿戴新装备，使用Item的SlotIndex记录槽位类型
            item.SlotIndex = (int)slotType;
            self.EquippedItems[slotType] = item;
        }

        /// <summary>
        /// 卸下指定槽位的装备（解除子实体挂载但不销毁 Item 实体，以便移回背包）
        /// </summary>
        /// <param name="self">装备管理组件实例</param>
        /// <param name="slotType">要卸下的装备槽位类型</param>
        public static void UnEquipItem(this EquipmentComponent self, EquipmentSlotType slotType)
        {
            if (self.EquippedItems.ContainsKey(slotType))
            {
                Item item = self.EquippedItems[slotType];
                if (item != null)
                {
                    // 设置SlotIndex为-1表示未装备
                    item.SlotIndex = -1;
                }
                item.RemoveComponent<EquipmentItemComponent>();
                // 仅从子节点集合移除而不 Dispose，后续会重新挂载回背包组件
                self.RemoveChild(item.Id, false);
                self.EquippedItems.Remove(slotType);
            }
        }

        /// <summary>
        /// 获取指定槽位当前穿戴的装备 Item 实体
        /// </summary>
        /// <param name="self">装备管理组件实例</param>
        /// <param name="slotType">装备槽位类型</param>
        /// <returns>穿戴的物品实体，若未穿戴则返回 null</returns>
        public static Item GetEquippedItem(this EquipmentComponent self, EquipmentSlotType slotType)
        {
            if (self.EquippedItems.TryGetValue(slotType, out EntityRef<Item> itemRef))
            {
                return itemRef;
            }
            return null;
        }

        /// <summary>
        /// 检查指定槽位是否已穿戴有效装备
        /// </summary>
        /// <param name="self">装备管理组件实例</param>
        /// <param name="slotType">装备槽位类型</param>
        /// <returns>若槽位上有装备则返回 true，否则返回 false</returns>
        public static bool HasEquippedItem(this EquipmentComponent self, EquipmentSlotType slotType)
        {
            if (self.EquippedItems.TryGetValue(slotType, out EntityRef<Item> itemRef))
            {
                Item item = itemRef;
                return item != null;
            }
            return false;
        }

        /// <summary>
        /// 获取当前所有已穿戴的有效装备总数
        /// </summary>
        /// <param name="self">装备管理组件实例</param>
        /// <returns>已穿戴装备数量</returns>
        public static int GetEquippedItemCount(this EquipmentComponent self)
        {
            int count = 0;
            foreach (var kvp in self.EquippedItems)
            {
                Item item = kvp.Value;
                if (item != null)
                {
                    ++count;
                }
            }
            return count;
        }

        /// <summary>
        /// 清空所有装备槽位映射并重置装备槽位索引
        /// </summary>
        /// <param name="self">装备管理组件实例</param>
        public static void ClearAllEquipment(this EquipmentComponent self)
        {
            foreach (var kvp in self.EquippedItems)
            {
                Item item = kvp.Value;
                if (item != null)
                {
                    // 设置SlotIndex为-1表示未装备
                    item.SlotIndex = -1;
                }
            }
            self.EquippedItems.Clear();
        }

        #endregion
    }
}
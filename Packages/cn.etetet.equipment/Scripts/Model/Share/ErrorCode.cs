namespace ET
{
    /// <summary>
    /// 装备模块错误码定义（基于 <see cref="PackageType.Equipment"/> = 47 偏移段）
    /// </summary>
    public static partial class ErrorCode
    {
        /// <summary>
        /// 装备槽位不存在
        /// </summary>
        public const int ERR_EquipmentSlotNotFound = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 1; // 200047001

        /// <summary>
        /// 指定的装备不存在
        /// </summary>
        public const int ERR_EquipmentNotFound = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 2; // 200047002

        /// <summary>
        /// 装备已处于穿戴状态
        /// </summary>
        public const int ERR_EquipmentAlreadyEquipped = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 3; // 200047003

        /// <summary>
        /// 该物品无法作为装备穿戴
        /// </summary>
        public const int ERR_EquipmentCannotEquip = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 4; // 200047004

        /// <summary>
        /// 角色等级不足，无法穿戴该装备
        /// </summary>
        public const int ERR_EquipmentLevelInsufficient = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 5; // 200047005

        /// <summary>
        /// 要穿戴的装备不在背包中
        /// </summary>
        public const int ERR_EquipmentItemNotInBag = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 6; // 200047006

        /// <summary>
        /// 卸下装备时背包已满
        /// </summary>
        public const int ERR_EquipmentBagFull = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 7; // 200047007

        /// <summary>
        /// 目标装备槽位为空，无装备可卸下
        /// </summary>
        public const int ERR_EquipmentSlotEmpty = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 8; // 200047008

        /// <summary>
        /// 非法的装备槽位类型
        /// </summary>
        public const int ERR_EquipmentInvalidSlotType = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 9; // 200047009

        /// <summary>
        /// 未找到背包组件（ItemComponent）
        /// </summary>
        public const int ERR_ItemComponentNotFound = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 10; // 200047010
    }
}
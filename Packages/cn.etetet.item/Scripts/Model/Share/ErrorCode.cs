namespace ET
{
    /// <summary>
    /// 背包与物品模块（Item）错误码常量定义。
    /// </summary>
    public static partial class ErrorCode
    {
        /// <summary>未找到指定物品</summary>
        public const int ERR_ItemNotFound = ErrorCode.ERR_WithoutException + PackageType.Item * 1000 + 1; // 200046001
        /// <summary>物品数量不足</summary>
        public const int ERR_ItemNotEnough = ErrorCode.ERR_WithoutException + PackageType.Item * 1000 + 2; // 200046002
        /// <summary>使用或丢弃的物品数量无效（&lt;= 0）</summary>
        public const int ERR_ItemUseCountInvalid = ErrorCode.ERR_WithoutException + PackageType.Item * 1000 + 3; // 200046003
        /// <summary>添加物品失败</summary>
        public const int ERR_ItemAddFailed = ErrorCode.ERR_WithoutException + PackageType.Item * 1000 + 4; // 200046004
        /// <summary>使用物品失败</summary>
        public const int ERR_ItemUseFailed = ErrorCode.ERR_WithoutException + PackageType.Item * 1000 + 5; // 200046005
        /// <summary>背包槽位索引非法</summary>
        public const int ERR_ItemSlotInvalid = ErrorCode.ERR_WithoutException + PackageType.Item * 1000 + 6; // 200046006
        /// <summary>物品不可堆叠</summary>
        public const int ERR_ItemCannotStack = ErrorCode.ERR_WithoutException + PackageType.Item * 1000 + 7; // 200046007
        /// <summary>不能将物品移动到原槽位</summary>
        public const int ERR_ItemMoveToSameSlot = ErrorCode.ERR_WithoutException + PackageType.Item * 1000 + 8; // 200046008
    }
}
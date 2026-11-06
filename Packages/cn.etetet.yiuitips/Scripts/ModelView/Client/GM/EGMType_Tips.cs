namespace ET.Client
{
    /// <summary>
    /// GM命令分组类型扩展（提示窗口分组）。
    /// </summary>
    public static partial class EGMType
    {
        /// <summary>通用提示弹窗测试GM分组。</summary>
        [GMGroup("提示窗口")]
        public const int Tips = PackageType.YIUI * 1302 + 1;
    }
}
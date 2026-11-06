namespace ET.Client
{
    /// <summary>
    /// GM命令分组类型扩展（红点调试分组）。
    /// </summary>
    public static partial class EGMType
    {
        /// <summary>红点系统调试GM分组。</summary>
        [GMGroup("红点")]
        public const int RedDot = PackageType.YIUI * 1304 + 1;
    }
}
namespace ET.Client
{
    /// <summary>
    /// GM命令分组类型扩展（3D模型展示分组）。
    /// </summary>
    public static partial class EGMType
    {
        /// <summary>3D模型展示测试GM分组。</summary>
        [GMGroup("模型展示")]
        public const int Display3D = PackageType.YIUI * 1303 + 1;
    }
}
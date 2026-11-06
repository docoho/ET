namespace ET
{
    /// <summary>
    /// 场景类型常量扩展（NetInner 内网通信模块）。
    /// </summary>
    public static partial class SceneType
    {
        /// <summary>进程级内网通信纤程场景类型。</summary>
        public const int NetInner = PackageType.NetInner * 1000 + 1;
    }
}
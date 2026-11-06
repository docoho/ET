namespace ET
{
    /// <summary>
    /// 服务发现模块的场景类型（SceneType）常量定义。
    /// </summary>
    public static partial class SceneType
    {
        /// <summary>服务发现中心场景（支持多实例主备高可用）</summary>
        public const int ServiceDiscovery = PackageType.ServiceDiscovery * 1000 + 1;
        /// <summary>进程级服务发现代理场景（每个服务端进程固定启动一个）</summary>
        public const int ServiceDiscoveryAgent = PackageType.ServiceDiscovery * 1000 + 2;
    }
}

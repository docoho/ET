namespace ET
{
    /// <summary>
    /// 场景类型扩展定义（Map 与 MapManager 模块）。
    /// </summary>
    public partial class SceneType
    {
        /// <summary>
        /// 服务端地图场景类型。
        /// </summary>
        public const int Map = PackageType.Map * 1000 + 2;

        /// <summary>
        /// 客户端根场景类型。
        /// </summary>
        public const int Client = PackageType.Map * 1000 + 3;

        /// <summary>
        /// 客户端当前活动地图子场景类型。
        /// </summary>
        public const int Current = PackageType.Map * 1000 + 1;

        /// <summary>
        /// 服务端地图副本与分线管理场景类型。
        /// </summary>
        public const int MapManager = PackageType.MapManager * 1000 + 1;
    }
}

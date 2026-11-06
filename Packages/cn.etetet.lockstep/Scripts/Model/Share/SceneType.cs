namespace ET
{
    /// <summary>
    /// 帧同步（LockStep）模块专属场景类型标识定义
    /// </summary>
    public static partial class SceneType
    {
        /// <summary>
        /// 服务端单局房间根场景类型
        /// </summary>
        public const int RoomRoot = PackageType.LockStep * 1000 + 1;

        /// <summary>
        /// 帧同步主入口场景类型
        /// </summary>
        public const int LockStep = PackageType.LockStep * 1000 + 2;

        /// <summary>
        /// 匹配服务场景类型
        /// </summary>
        public const int Match = PackageType.LockStep * 1000 + 3;

        /// <summary>
        /// 帧同步地图房间管理服务场景类型
        /// </summary>
        public const int Map = PackageType.LockStep * 1000 + 4;

        /// <summary>
        /// 服务端帧同步逻辑世界（LSWorld）场景类型
        /// </summary>
        public const int LockStepServer = PackageType.LockStep * 1000 + 5;
        
        /// <summary>
        /// 客户端帧同步逻辑世界（LSWorld）场景类型
        /// </summary>
        public const int LockStepClient = PackageType.LockStep * 1000 + 21;
        
    }
}
namespace ET
{
    /// <summary>
    /// 服务发现模块专用的协程锁类型常量定义。
    /// </summary>
    public static partial class CoroutineLockType
    {
        /// <summary>进程级 ServiceDiscoveryAgent 初始解析主节点与全量注册互斥锁</summary>
        public const int ServiceDiscoveryAgentInit = PackageType.ServiceDiscovery * 1000 + 5; // ServiceDiscoveryAgent显式初始化
        /// <summary>ServiceDiscovery 主节点上按服务名（SceneName）串行化注册/注销操作的协程锁</summary>
        public const int ServiceDiscoveryServiceMutation = PackageType.ServiceDiscovery * 1000 + 6; // ServiceDiscovery服务注册/注销串行化
        /// <summary>ServiceDiscovery 主备节点租约续期与选主接管状态切换串行化协程锁</summary>
        public const int ServiceDiscoveryMasterLease = PackageType.ServiceDiscovery * 1000 + 9; // ServiceDiscovery主租约状态切换串行化
    }
}

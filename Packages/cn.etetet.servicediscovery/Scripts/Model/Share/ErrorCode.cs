namespace ET
{
    /// <summary>
    /// 服务发现模块错误码定义（均为不抛异常的业务错误码段）。
    /// </summary>
    public static partial class ErrorCode
    {
        /// <summary>未找到必需的服务发现相关组件</summary>
        public const int ERR_ComponentNotFound = ErrorCode.ERR_WithoutException + PackageType.ServiceDiscovery * 1000 + 1; // 200050001
        /// <summary>在本地缓存中未找到匹配的目标服务</summary>
        public const int ERR_ServiceNotFound = ErrorCode.ERR_WithoutException + PackageType.ServiceDiscovery * 1000 + 2; // 200050002
        /// <summary>服务已存在</summary>
        public const int ERR_ServiceAlreadyExists = ErrorCode.ERR_WithoutException + PackageType.ServiceDiscovery * 1000 + 3; // 200050003
        /// <summary>订阅请求缺少必填的 SubscriberActorId</summary>
        public const int ERR_SubscriberActorIdRequired = ErrorCode.ERR_WithoutException + PackageType.ServiceDiscovery * 1000 + 4; // 200050004
        /// <summary>服务发现内部操作执行失败</summary>
        public const int ERR_ServiceDiscoveryOperationFailed = ErrorCode.ERR_WithoutException + PackageType.ServiceDiscovery * 1000 + 5; // 200050005
        /// <summary>当前访问的节点为备节点（Follower），拒绝写入并提示触发主节点重解析</summary>
        public const int ERR_ServiceDiscoveryFollowerRejected = ErrorCode.ERR_WithoutException + PackageType.ServiceDiscovery * 1000 + 6; // 200050006
        /// <summary>服务发现请求参数非法</summary>
        public const int ERR_ServiceDiscoveryInvalidArgument = ErrorCode.ERR_WithoutException + PackageType.ServiceDiscovery * 1000 + 7; // 200050007
        /// <summary>服务发现状态持久化或数据库操作失败</summary>
        public const int ERR_ServiceDiscoveryPersistenceFailed = ErrorCode.ERR_WithoutException + PackageType.ServiceDiscovery * 1000 + 8; // 200050008
        /// <summary>当前无可用活跃主服务发现节点</summary>
        public const int ERR_ServiceDiscoveryMasterUnavailable = ErrorCode.ERR_WithoutException + PackageType.ServiceDiscovery * 1000 + 9; // 200050009
    }
}

namespace ET
{
    /// <summary>
    /// 服务注册与订阅过滤常用的标准元数据键名（Metadata Key）常量。
    /// </summary>
    public static partial class ServiceMetaKey
    {
        /// <summary>场景类型名称（如 Gate、Map、Location）</summary>
        public const string SceneType = nameof(SceneType);
        /// <summary>所属区服编号（Zone）</summary>
        public const string Zone = nameof(Zone);
        /// <summary>服务 ActorId 标识</summary>
        public const string ActorId = nameof(ActorId);
        /// <summary>主备优先级编号（例如 Location 主备的 PriorityId）</summary>
        public const string PriorityId = nameof(PriorityId);
        /// <summary>内网 IP + 外网端口（供 Router 路由节点暴露给 RouterManager）</summary>
        public const string InnerIPOuterPort = nameof(InnerIPOuterPort);
        /// <summary>外网 IP + 外网端口（供客户端连接网关或路由节点）</summary>
        public const string OuterIPOuterPort = nameof(OuterIPOuterPort);
    }

    /// <summary>
    /// 服务发现主节点租约持久化到 MongoDB 的常量定义。
    /// </summary>
    public static class ServiceDiscoveryPersistenceConst
    {
        /// <summary>默认共享数据库名称</summary>
        public const string DBName = "Share";

        /// <summary>主节点租约单条记录的固定主键 ID</summary>
        public const long MasterRecordId = 1;

        /// <summary>主节点租约记录存储的 MongoDB 集合名称</summary>
        public const string MasterCollection = "ServiceDiscoveryMaster";

    }
}

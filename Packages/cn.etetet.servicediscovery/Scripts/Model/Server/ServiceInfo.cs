using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 服务信息实体，记录注册到服务发现中的Fiber信息
    /// </summary>
    [ChildOf]
    public class ServiceInfo : Entity, IAwake<string, ActorId>, IDestroy
    {
        /// <summary>
        /// 服务场景的唯一名称（如 Gate1、Map1）
        /// </summary>
        public string SceneName;

        /// <summary>
        /// Actor ID
        /// </summary>
        public ActorId ActorId;

        /// <summary>
        /// 服务元数据，存储KV键值对
        /// </summary>
        public StringKV Metadata = new();

        /// <summary>
        /// 注册时间
        /// </summary>
        public long RegisterTime;

        /// <summary>
        /// 最后心跳时间
        /// </summary>
        public long LastHeartbeatTime;
    }

    /// <summary>
    /// 持久化在 MongoDB 中的服务发现主节点租约记录实体。
    /// 通过 `FindOneAndReplace` 原子 CAS 更新以实现分布式主备选主与 Fencing Epoch 脑裂防护。
    /// </summary>
    [ComponentOf(typeof(ServiceDiscovery))]
    public class ServiceDiscoveryMaster : Entity, IAwake
    {
        /// <summary>当前持有租约的主节点场景名称</summary>
        public string SceneName;

        /// <summary>当前持有租约的主节点 ActorId</summary>
        public ActorId ActorId;

        /// <summary>单调递增的主节点任期号（每次新节点接管时 +1）</summary>
        public long Epoch;

        /// <summary>租约过期时间戳（毫秒）</summary>
        public long LeaseExpireTime;

        /// <summary>租约超时时长（毫秒）</summary>
        public long LeaseTimeout;

        /// <summary>最后一次续租或接管的更新时间戳（毫秒）</summary>
        public long UpdateTime;
    }

}

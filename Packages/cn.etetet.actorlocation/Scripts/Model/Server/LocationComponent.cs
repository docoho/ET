using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace ET.Server
{
    /// <summary>
    /// Location 位置路由持久化常量定义。
    /// </summary>
    public static class LocationPersistenceConst
    {
        /// <summary>
        /// 位置路由存储的 MongoDB 共享数据库名称。
        /// </summary>
        public const string DBName = "Share";

        /// <summary>
        /// 位置路由存储的 MongoDB 集合（表）名称。
        /// </summary>
        public const string RouteCollection = "LocationInfo";
    }

    /// <summary>
    /// 单个位置分类（LocationType）下的位置路由与锁状态。
    /// </summary>
    public struct LocationTypeState
    {
        /// <summary>
        /// 实体当前所在的 ActorId（包含进程、Fiber 与 InstanceId）。
        /// </summary>
        public ActorId ActorId;

        /// <summary>
        /// 位置锁令牌（> 0 表示当前处于锁定状态，解锁时必须校验此 Token）。
        /// </summary>
        public long LockToken;

        /// <summary>
        /// 位置锁过期时间戳（毫秒），超时后自动视为未锁定以防止死锁。
        /// </summary>
        public long LockExpireTime;
    }

    /// <summary>
    /// 实体位置路由信息实体（以实体的 Id 作为自身 Id，支持持久化到 MongoDB）。
    /// </summary>
    [ChildOf(typeof(LocationComponent))]
    public class LocationInfo: Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 按位置分类（LocationType，如 Unit、Player、GateSession 等）索引的位置与锁状态字典。
        /// </summary>
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        public Dictionary<int, LocationTypeState> TypeStates = new();
    }

    /// <summary>
    /// 位置定位服务核心组件（挂载在 Location 场景上，基于主备选举提供全局唯一写入口并持久化到 MongoDB）。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class LocationComponent: Entity, IAwake
    {
        /// <summary>
        /// 当前节点是否为主 Location 节点（只有主节点才能处理位置注册、加解锁与查询请求）。
        /// </summary>
        public bool IsPrimaryLocation;

        /// <summary>
        /// 当前选举出的主 Location 场景名称。
        /// </summary>
        public string PrimaryLocationSceneName;
    }
}

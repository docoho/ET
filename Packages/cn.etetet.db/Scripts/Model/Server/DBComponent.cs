using System.Collections.Generic;
using MongoDB.Driver;

namespace ET.Server
{
    /// <summary>
    /// 单个区服的 MongoDB 数据库连接与访问组件（作为 <see cref="DBManagerComponent"/> 的子实体按区服 ID 挂载）。
    /// <para>用来缓存数据并执行实体持久化读写。</para>
    /// </summary>
    [ChildOf(typeof(DBManagerComponent))]
    public class DBComponent: Entity, IAwake<string, string>
    {
        /// <summary>MongoDB 协程锁并发队列分片数量（用于对 Entity.Id 取模实现细粒度并发控制）。</summary>
        public const int TaskCount = 32;

        /// <summary>底层 MongoDB 客户端连接实例。</summary>
        public MongoClient mongoClient;
        /// <summary>当前区服对应的 MongoDB 数据库实例。</summary>
        public IMongoDatabase database;
        /// <summary>当前数据库中已存在的集合（表）名称缓存集合。</summary>
        public HashSet<string> CollectionDic = new();
    }
}
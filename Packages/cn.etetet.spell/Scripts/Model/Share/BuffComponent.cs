using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace ET
{
    /// <summary>
    /// 单位身上的 Buff 容器组件，管理所有挂载的 Buff 子实体，并维护按移除标记、效果节点类型和配置 ID 的多级索引以支持高效查询与事件触发。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class BuffComponent: Entity, IAwake, ITransfer, IDeserialize
    {
        /// <summary>
        /// 按 BuffFlags（移除条件标记）建立的 Buff 索引字典。
        /// </summary>
        [BsonIgnore]
        public MultiMapSet<int, EntityRef<Buff>> flagBuffs = new();

        // key是EffectNode Type
        [BsonIgnore]
        public MultiMapSet<Type, EntityRef<Buff>> effectBuffs = new();
        
        // key是BuffConfig Id
        [BsonIgnore]
        public MultiMapSet<int, EntityRef<Buff>> configIdBuffs = new();
    }
}
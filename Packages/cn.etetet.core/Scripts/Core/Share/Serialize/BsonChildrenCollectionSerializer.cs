using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace ET
{
    /// <summary>
    /// <see cref="ChildrenCollection"/> 的自定义 MongoDB BSON 序列化器。
    /// <remarks>
    /// 序列化时仅将标记了 <see cref="Entity.IsSerializeWithParent"/> 为 <c>true</c>（即通过 <c>AddChildWithSerialize</c> 添加）的子实体写入 BSON 数组；
    /// 反序列化时从对象池创建 <see cref="ChildrenCollection"/>，逐个还原子实体并以其 <c>Id</c> 为键插入集合。
    /// </remarks>
    /// </summary>
    public class BsonChildrenCollectionSerializer: IBsonSerializer
    {
        /// <summary>
        /// 从 BSON 数组中反序列化出 <see cref="ChildrenCollection"/> 及其包含的所有子实体。
        /// </summary>
        public object Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            ChildrenCollection childrenCollection = ChildrenCollection.Create(true);
            IBsonSerializer<Entity> bsonSerializer = BsonSerializer.LookupSerializer<Entity>();
            var bsonReader = context.Reader;
            bsonReader.ReadStartArray();
            while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
            {
                Entity entity = bsonSerializer.Deserialize(context);
                entity.IsSerializeWithParent = true;
                childrenCollection.Add(entity.Id, entity);
            }
            bsonReader.ReadEndArray();

            return childrenCollection;
        }

        /// <summary>
        /// 将 <see cref="ChildrenCollection"/> 中所有需要随父级持久化的子实体序列化为 BSON 数组。
        /// </summary>
        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            IBsonWriter bsonWriter = context.Writer;
            bsonWriter.WriteStartArray();
            ChildrenCollection childrenCollection = (ChildrenCollection)value;

            IBsonSerializer<Entity> bsonSerializer = BsonSerializer.LookupSerializer<Entity>();
            foreach ((long _, Entity entity) in childrenCollection)
            {
                if (entity.IsSerializeWithParent)
                {
                    bsonSerializer.Serialize(context, entity);
                }
            }
            bsonWriter.WriteEndArray();
        }

        public System.Type ValueType { get; }
    }
}
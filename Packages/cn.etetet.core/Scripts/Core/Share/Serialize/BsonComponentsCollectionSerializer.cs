using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace ET
{
    /// <summary>
    /// <see cref="ComponentsCollection"/> 的自定义 MongoDB BSON 序列化器。
    /// <remarks>
    /// 序列化时仅将标记了 <see cref="Entity.IsSerializeWithParent"/> 为 <c>true</c>（即实现了 <see cref="ISerializeToEntity"/> 接口）的组件写入 BSON 数组；
    /// 反序列化时从对象池创建 <see cref="ComponentsCollection"/>，逐个还原组件并以其类型长哈希（<c>GetLongHashCode()</c>）为键插入集合。
    /// </remarks>
    /// </summary>
    public class BsonComponentsCollectionSerializer: IBsonSerializer
    {
        /// <summary>
        /// 从 BSON 数组中反序列化出 <see cref="ComponentsCollection"/> 及其包含的所有持久化组件。
        /// </summary>
        public object Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            ComponentsCollection componentsCollection = ComponentsCollection.Create(true);
            IBsonSerializer<Entity> bsonSerializer = BsonSerializer.LookupSerializer<Entity>();
            IBsonReader bsonReader = context.Reader;
            bsonReader.ReadStartArray();
            while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
            {
                Entity entity = bsonSerializer.Deserialize(context);
                entity.IsSerializeWithParent = true;
                componentsCollection.Add(entity.GetLongHashCode(), entity);
            }
            bsonReader.ReadEndArray();

            return componentsCollection;
        }

        /// <summary>
        /// 将 <see cref="ComponentsCollection"/> 中所有需要随父实体持久化的组件序列化为 BSON 数组。
        /// </summary>
        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            IBsonWriter bsonWriter = context.Writer;
            bsonWriter.WriteStartArray();
            ComponentsCollection componentsCollection = (ComponentsCollection)value;

            IBsonSerializer<Entity> bsonSerializer = BsonSerializer.LookupSerializer<Entity>();
            foreach ((long _, Entity entity) in componentsCollection)
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
using System;
using System.Reflection;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization.Attributes;

namespace ET
{
    /// <summary>
    /// 通用值类型（<c>struct</c>）的反射式 MongoDB BSON 序列化器。
    /// <remarks>
    /// MongoDB C# Driver 默认不支持自定义结构体的自动映射；此类通过反射遍历结构体的公共字段或标记了 <see cref="BsonElementAttribute"/> 的私有字段完成读写。
    /// </remarks>
    /// </summary>
    public class StructBsonSerialize<TValue>: StructSerializerBase<TValue> where TValue : struct
    {
        /// <summary>
        /// 将结构体字段序列化为 BSON 文档。
        /// </summary>
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TValue value)
        {
            Type nominalType = args.NominalType;

            IBsonWriter bsonWriter = context.Writer;

            bsonWriter.WriteStartDocument();

            FieldInfo[] fields = nominalType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (FieldInfo field in fields)
            {
                BsonElementAttribute bsonElement = field.GetCustomAttribute<BsonElementAttribute>();
                if (bsonElement == null && !field.IsPublic)
                {
                    continue;
                }
                bsonWriter.WriteName(field.Name);
                BsonSerializer.Serialize(bsonWriter, field.FieldType, field.GetValue(value)); 
            }

            bsonWriter.WriteEndDocument();
        }

        /// <summary>
        /// 从 BSON 文档中反序列化还原结构体实例（利用装箱后的 <c>object</c> 调用 <see cref="FieldInfo.SetValue(object, object)"/> 修改字段值）。
        /// </summary>
        public override TValue Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            //boxing is required for SetValue to work
            object obj = new TValue();
            Type actualType = args.NominalType;
            IBsonReader bsonReader = context.Reader;

            bsonReader.ReadStartDocument();

            while (bsonReader.State != BsonReaderState.EndOfDocument)
            {
                switch (bsonReader.State)
                {
                    case BsonReaderState.Name:
                    {
                        string name = bsonReader.ReadName(Utf8NameDecoder.Instance);
                        FieldInfo field = actualType.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (field != null)
                        {
                            object value = BsonSerializer.Deserialize(bsonReader, field.FieldType);
                            field.SetValue(obj, value);
                        }

                        break;
                    }
                    case BsonReaderState.Type:
                    {
                        bsonReader.ReadBsonType();
                        break;
                    }
                    case BsonReaderState.Value:
                    {
                        bsonReader.SkipValue();
                        break;
                    }
                }
            }

            bsonReader.ReadEndDocument();

            return (TValue)obj;
        }
    }
}
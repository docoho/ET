using System;
using System.ComponentModel;
using System.IO;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace ET
{
    /// <summary>
    /// MongoDB BSON 与 JSON 序列化、反序列化及深拷贝辅助工具类。
    /// <remarks>
    /// 在 ET 中廣泛用于：数据库实体存取、实体跨进程传送（Transfer）序列化、调试日志对象转 JSON 打印等。
    /// </remarks>
    /// </summary>
    public static class MongoHelper
    {
        /// <summary>
        /// 默认 JSON 输出配置（宽松扩展 JSON 模式，适合日志打印）。
        /// </summary>
        [StaticField]
        private static readonly JsonWriterSettings defaultSettings = new() { OutputMode = JsonOutputMode.RelaxedExtendedJson };
        
        /// <summary>
        /// 配置文件导出专用的格式化 JSON 输出配置（带制表符缩进与换行）。
        /// </summary>
        [StaticField]
        public static readonly JsonWriterSettings ConfigSettings = new() 
        { 
            Indent = true, 
            IndentChars = "\t", 
            NewLineChars = "\n", 
            OutputMode = JsonOutputMode.Shell 
        };
        
        /// <summary>
        /// 将对象序列化为默认格式的 JSON 字符串。
        /// </summary>
        public static string ToJson(object obj)
        {
            try
            {
                if (obj is ISupportInitialize supportInitialize)
                {
                    supportInitialize.BeginInit();
                }
                return obj.ToJson(defaultSettings);
            }
            catch (Exception e)
            {
                throw new Exception($"to json error {obj.GetType().FullName}\n{e}");
            }
        }

        /// <summary>
        /// 使用指定的 <see cref="JsonWriterSettings"/> 将对象序列化为 JSON 字符串。
        /// </summary>
        public static string ToJson(object obj, JsonWriterSettings settings)
        {
            try
            {
                if (obj is ISupportInitialize supportInitialize)
                {
                    supportInitialize.BeginInit();
                }
                return obj.ToJson(settings);
            }
            catch (Exception e)
            {
                throw new Exception($"to json error {obj.GetType().FullName}\n{e}");
            }
        }

        /// <summary>
        /// 将 JSON 字符串反序列化为指定泛型类型 <typeparamref name="T"/> 的实例。
        /// </summary>
        public static T FromJson<T>(string str)
        {
            try
            {
                return BsonSerializer.Deserialize<T>(str);
            }
            catch (Exception e)
            {
                throw new Exception($"from json error: {str}\n{e}");
            }
        }

        /// <summary>
        /// 将 JSON 字符串反序列化为指定 <see cref="Type"/> 的实例。
        /// </summary>
        public static object FromJson(Type type, string str)
        {
            try
            {
                return BsonSerializer.Deserialize(str, type);
            }
            catch (Exception e)
            {
                throw new Exception($"from json error: {str}\n{e}");
            }
        }

        /// <summary>
        /// 将对象序列化为 BSON 二进制字节数组。
        /// </summary>
        public static byte[] Serialize(object obj)
        {
            try
            {
                if (obj is ISupportInitialize supportInitialize)
                {
                    supportInitialize.BeginInit();
                }
                return obj.ToBson();
            }
            catch (Exception e)
            {
                throw new Exception($"Serialize error {obj.GetType().FullName}\n{e}");
            }
        }

        /// <summary>
        /// 将对象以 BSON 二进制格式直接序列化写入指定的 <see cref="MemoryStream"/> 中。
        /// </summary>
        public static void Serialize(object message, MemoryStream stream)
        {
            try
            {
                if (message is ISupportInitialize supportInitialize)
                {
                    supportInitialize.BeginInit();
                }

                using BsonBinaryWriter bsonWriter = new(stream, BsonBinaryWriterSettings.Defaults);
            
                BsonSerializationContext context = BsonSerializationContext.CreateRoot(bsonWriter);
                BsonSerializationArgs args = default;
                args.NominalType = typeof (object);
                IBsonSerializer serializer = BsonSerializer.LookupSerializer(args.NominalType);
                serializer.Serialize(context, args, message);
            }
            catch (Exception e)
            {
                throw new Exception($"Serialize error {message.GetType().FullName}\n{e}");
            }
        }

        /// <summary>
        /// 从 BSON 字节数组中反序列化出指定 <see cref="Type"/> 的对象。
        /// </summary>
        public static object Deserialize(Type type, byte[] bytes)
        {
            try
            {
                return BsonSerializer.Deserialize(bytes, type);
            }
            catch (Exception e)
            {
                throw new Exception($"from bson error: {type.FullName} {bytes.Length}", e);
            }
        }

        /// <summary>
        /// 从 BSON 字节数组的指定切片区间反序列化出指定 <see cref="Type"/> 的对象。
        /// </summary>
        public static object Deserialize(Type type, byte[] bytes, int index, int count)
        {
            try
            {
                using MemoryStream memoryStream = new(bytes, index, count);
                
                return BsonSerializer.Deserialize(memoryStream, type);
            }
            catch (Exception e)
            {
                throw new Exception($"from bson error: {type.FullName} {bytes.Length} {index} {count}", e);
            }
        }

        /// <summary>
        /// 从流（<see cref="Stream"/>）中反序列化出指定 <see cref="Type"/> 的对象。
        /// </summary>
        public static object Deserialize(Type type, Stream stream)
        {
            try
            {
                return BsonSerializer.Deserialize(stream, type);
            }
            catch (Exception e)
            {
                throw new Exception($"from bson error: {type.FullName} {stream.Position} {stream.Length}", e);
            }
        }

        /// <summary>
        /// 从 BSON 字节数组中反序列化出指定泛型类型 <typeparamref name="T"/> 的对象。
        /// </summary>
        public static T Deserialize<T>(byte[] bytes)
        {
            try
            {
                using MemoryStream memoryStream = new(bytes);
                
                return (T)BsonSerializer.Deserialize(memoryStream, typeof (T));
            }
            catch (Exception e)
            {
                throw new Exception($"from bson error: {typeof (T).FullName} {bytes.Length}", e);
            }
        }

        /// <summary>
        /// 从 BSON 字节数组的指定切片区间反序列化出指定泛型类型 <typeparamref name="T"/> 的对象。
        /// </summary>
        public static T Deserialize<T>(byte[] bytes, int index, int count)
        {
            return (T)Deserialize(typeof (T), bytes, index, count);
        }

        /// <summary>
        /// 通过 BSON 序列化再反序列化的方式对对象进行深拷贝（Clone）。
        /// </summary>
        public static T Clone<T>(T t)
        {
            return Deserialize<T>(Serialize(t));
        }
    }
}
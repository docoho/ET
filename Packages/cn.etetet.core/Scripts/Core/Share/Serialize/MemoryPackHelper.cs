using System;
using System.ComponentModel;
using MemoryPack;

namespace ET
{
    /// <summary>
    /// MemoryPack 高性能二进制序列化与反序列化封装工具类。
    /// <remarks>
    /// 在序列化前自动触发 <see cref="ISupportInitialize.BeginInit"/>（如 <see cref="Entity"/> 的 <c>ISerialize</c> 钩子），
    /// 在反序列化后自动触发 <see cref="ISupportInitialize.EndInit"/>（如重建实体父子引用或池状态）。
    /// </remarks>
    /// </summary>
    public static class MemoryPackHelper
    {
        /// <summary>
        /// 将对象序列化为字节数组。
        /// </summary>
        public static byte[] Serialize(object message)
        {
            if (message is ISupportInitialize supportInitialize)
            {
                supportInitialize.BeginInit();
            }
            return MemoryPackSerializer.Serialize(message.GetType(), message);
        }

        /// <summary>
        /// 将对象直接序列化写入指定的 <see cref="MemoryBuffer"/> 流中（零额外数组分配）。
        /// </summary>
        public static void Serialize(object message, MemoryBuffer stream)
        {
            if (message is ISupportInitialize supportInitialize)
            {
                supportInitialize.BeginInit();
            }
            MemoryPackSerializer.Serialize(message.GetType(), stream, message);
        }
        
        /// <summary>
        /// 从字节数组的指定区间反序列化出指定类型的新对象。
        /// </summary>
        public static object Deserialize(Type type, byte[] bytes, int index, int count)
        {
            object o = MemoryPackSerializer.Deserialize(type, bytes.AsSpan(index, count));
            if (o is ISupportInitialize supportInitialize)
            {
                supportInitialize.EndInit();
            }
            return o;
        }

        /// <summary>
        /// 从字节数组的指定区间反序列化并填充到已有的对象引用 <paramref name="o"/> 中（配合对象池复用消息实例）。
        /// </summary>
        public static object Deserialize(Type type, byte[] bytes, int index, int count, ref object o)
        {
            MemoryPackSerializer.Deserialize(type, bytes.AsSpan(index, count), ref o);
            if (o is ISupportInitialize supportInitialize)
            {
                supportInitialize.EndInit();
            }
            return o;
        }

        /// <summary>
        /// 从 <see cref="MemoryBuffer"/> 当前可读切片中反序列化出指定类型的新对象。
        /// </summary>
        public static object Deserialize(Type type, MemoryBuffer stream)
        {
            object o = MemoryPackSerializer.Deserialize(type, stream.GetSpan());
            if (o is ISupportInitialize supportInitialize)
            {
                supportInitialize.EndInit();
            }
            return o;
        }

        /// <summary>
        /// 从 <see cref="MemoryBuffer"/> 当前可读切片中反序列化并填充到已有的对象引用 <paramref name="o"/> 中。
        /// </summary>
        public static object Deserialize(Type type, MemoryBuffer stream, ref object o)
        {
            MemoryPackSerializer.Deserialize(type, stream.GetSpan(), ref o);
            if (o is ISupportInitialize supportInitialize)
            {
                supportInitialize.EndInit();
            }
            return o;
        }
    }
}
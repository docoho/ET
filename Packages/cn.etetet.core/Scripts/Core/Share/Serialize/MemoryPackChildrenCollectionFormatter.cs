using MemoryPack.Internal;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MemoryPack;
using MemoryPack.Formatters;

#nullable enable
namespace ET
{
    /// <summary>
    /// 替换默认的SortedDictionaryFormatter，为 <see cref="ChildrenCollection"/> 提供高性能 MemoryPack 二进制序列化器。
    /// <remarks>
    /// 序列化时预先在头部预留 4 字节集合长度槽位，遍历集合仅序列化 <see cref="Entity.IsSerializeWithParent"/> 为 <c>true</c> 的子实体，最后回填实际写入的个数 <c>count</c>。
    /// </remarks>
    /// </summary>
    [Preserve]
    public sealed class MemoryPackChildrenCollectionFormatter : MemoryPackFormatter<ChildrenCollection>
    {
        /// <summary>
        /// 将 <see cref="ChildrenCollection"/> 中需要持久化的子实体序列化写入 <see cref="MemoryPackWriter{TBufferWriter}"/>。
        /// </summary>
        [Preserve]
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, ref ChildrenCollection? value)
        {
            if (value == null)
            {
                writer.WriteNullCollectionHeader();
                return;
            }
            
            // writer.WriteCollectionHeader(value.Count);
            var formatter = writer.GetFormatter<Entity>();
            // 先预留 4 字节用于回填实际序列化的子实体数量
            ref byte spanReference = ref writer.GetSpanReference(4);
            writer.Advance(4);
            int count = 0;
            foreach (var kv in value)
            {
                Entity entity = kv.Value;
                if (entity.IsSerializeWithParent)
                {
                    ++count;
                    formatter.Serialize(ref writer, ref entity!);
                }
            }
            SafeUnsafe.WriteUnaligned(ref spanReference, count);
        }

        /// <summary>
        /// 从 <see cref="MemoryPackReader"/> 中反序列化还原 <see cref="ChildrenCollection"/> 及其子实体。
        /// </summary>
        [Preserve]
        public override void Deserialize(ref MemoryPackReader reader, ref ChildrenCollection? value)
        {
            if (!reader.TryReadCollectionHeader(out int length))
            {
                value = null;
                return;
            }

            if (value == null)
            {
                value = ChildrenCollection.Create(true);
            }
            else
            {
                value.Clear();
            }

            var formatter = reader.GetFormatter<Entity>();
            for (int i = 0; i < length; i++)
            {
                Entity entity = null!;
                formatter.Deserialize(ref reader, ref entity!);
                entity.IsSerializeWithParent = true;
                value.Add(entity.Id, entity);
            }
        }
    }
}
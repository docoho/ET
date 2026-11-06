using System;
using System.Buffers;
using System.IO;

namespace ET
{
    /// <summary>
    /// 扩展自 <see cref="MemoryStream"/> 并实现了 <see cref="IBufferWriter{T}"/> 接口的内存缓冲区。
    /// <remarks>
    /// 核心作用：桥接传统的 <see cref="Stream"/> 读写（如 Socket/KCP 数据包缓冲）与现代高性能零拷贝序列化器（如 MemoryPack 的 <see cref="IBufferWriter{Byte}"/>），
    /// 支持直接暴露底层字节数组的 <see cref="Span{Byte}"/> 与 <see cref="Memory{Byte}"/>，避免二次拷贝。
    /// </remarks>
    /// </summary>
    public class MemoryBuffer: MemoryStream, IBufferWriter<byte>
    {
        /// <summary>
        /// 外部传入字节数组切片时的起始偏移量。
        /// </summary>
        private int origin;
        
        public MemoryBuffer()
        {
        }
        
        public MemoryBuffer(int capacity): base(capacity)
        {
        }
        
        public MemoryBuffer(byte[] buffer): base(buffer)
        {
        } 
        
        public MemoryBuffer(byte[] buffer, int index, int length): base(buffer, index, length)
        {
            this.origin = index;
        }
        
        /// <summary>
        /// 获取从起始偏移到当前写入位置（<see cref="MemoryStream.Position"/>）的只读内存切片。
        /// </summary>
        public ReadOnlyMemory<byte> WrittenMemory => this.GetBuffer().AsMemory(this.origin, (int)this.Position);

        /// <summary>
        /// 获取从起始偏移到当前写入位置（<see cref="MemoryStream.Position"/>）的只读 Span 切片。
        /// </summary>
        public ReadOnlySpan<byte> WrittenSpan => this.GetBuffer().AsSpan(this.origin, (int)this.Position);

        /// <summary>
        /// 通知缓冲区已向 <see cref="GetMemory"/> 或 <see cref="GetSpan"/> 写入了 <paramref name="count"/> 个字节，推进 <see cref="MemoryStream.Position"/>。
        /// </summary>
        public void Advance(int count)
        {
            long newLength = this.Position + count;
            if (newLength > this.Length)
            {
                this.SetLength(newLength);
            }
            this.Position = newLength;
        }

        /// <summary>
        /// 获取从当前 <see cref="MemoryStream.Position"/> 开始、至少包含 <paramref name="sizeHint"/> 字节的可写 <see cref="Memory{Byte}"/>（不足时自动扩容）。
        /// </summary>
        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            if (this.Length - this.Position < sizeHint)
            {
                this.SetLength(this.Position + sizeHint);
            }
            var memory = this.GetBuffer().AsMemory((int)this.Position + this.origin, (int)(this.Length - this.Position));
            return memory;
        }

        /// <summary>
        /// 获取从当前 <see cref="MemoryStream.Position"/> 开始、至少包含 <paramref name="sizeHint"/> 字节的可写 <see cref="Span{Byte}"/>（不足时自动扩容）。
        /// </summary>
        public Span<byte> GetSpan(int sizeHint = 0)
        {
            if (this.Length - this.Position < sizeHint)
            {
                this.SetLength(this.Position + sizeHint);
            }
            var span = this.GetBuffer().AsSpan((int)this.Position + this.origin, (int)(this.Length - this.Position));
            return span;
        }
    }
}
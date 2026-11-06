using System;
using System.Collections.Generic;
using System.IO;

namespace ET
{
    /// <summary>
    /// 基于固定大小分块队列（Chunk Queue）的环形流式字节缓冲区，主要用于 TCP 通道（<see cref="TChannel"/>）的粘包/半包流式读写。
    /// <remarks>
    /// 工作原理：
    /// 1. 内部维护一个存放固定长度（<see cref="ChunkSize"/> = 8192 字节）<c>byte[]</c> 块的活跃队列 <see cref="bufferQueue"/> 以及空闲块复用队列 <see cref="bufferCache"/>；
    /// 2. <see cref="FirstIndex"/> 指向队首数据块的当前读取偏移，<see cref="LastIndex"/> 指向队尾数据块的当前写入偏移；
    /// 3. 当队首块被读完（<c>FirstIndex == ChunkSize</c>）时，自动调用 <see cref="RemoveFirst"/> 将其移入 <see cref="bufferCache"/> 供后续写入复用，从而彻底消除大缓冲区扩容和内存搬移开销。
    /// </remarks>
    /// </summary>
    public class CircularBuffer: Stream
    {
        /// <summary>
        /// 单个内存块的固定字节大小（默认 8KB）。
        /// </summary>
        public int ChunkSize = 8192;

        /// <summary>
        /// 正在承载未读数据的内存块队列。
        /// </summary>
        private readonly Queue<byte[]> bufferQueue = new Queue<byte[]>();

        /// <summary>
        /// 已读完并回收待复用的空闲内存块缓存池。
        /// </summary>
        private readonly Queue<byte[]> bufferCache = new Queue<byte[]>();

        /// <summary>
        /// 队尾内存块（<see cref="Last"/>）的当前写入位置偏移（0 ~ <see cref="ChunkSize"/>）。
        /// </summary>
        public int LastIndex { get; set; }

        /// <summary>
        /// 队首内存块（<see cref="First"/>）的当前读取位置偏移（0 ~ <see cref="ChunkSize"/>）。
        /// </summary>
        public int FirstIndex { get; set; }
		
        private byte[] lastBuffer;

	    public CircularBuffer()
	    {
		    this.AddLast();
	    }

        /// <summary>
        /// 获取当前缓冲区中尚未读取的可读总字节数。
        /// </summary>
        public override long Length
        {
            get
            {
                int c = 0;
                if (this.bufferQueue.Count == 0)
                {
                    c = 0;
                }
                else
                {
                    c = (this.bufferQueue.Count - 1) * ChunkSize + this.LastIndex - this.FirstIndex;
                }
                if (c < 0)
                {
					Log.Error("CircularBuffer count < 0: {0}, {1}, {2}".Fmt(this.bufferQueue.Count, this.LastIndex, this.FirstIndex));
                }
                return c;
            }
        }

        /// <summary>
        /// 向队尾追加一个新的内存块（优先从 <see cref="bufferCache"/> 缓存池复用）。
        /// </summary>
        public void AddLast()
        {
            byte[] buffer;
            if (this.bufferCache.Count > 0)
            {
                buffer = this.bufferCache.Dequeue();
            }
            else
            {
                buffer = new byte[ChunkSize];
            }
            this.bufferQueue.Enqueue(buffer);
            this.lastBuffer = buffer;
        }

        /// <summary>
        /// 移除已读完的队首内存块并将其回收到 <see cref="bufferCache"/> 缓存池。
        /// </summary>
        public void RemoveFirst()
        {
            this.bufferCache.Enqueue(bufferQueue.Dequeue());
        }

        /// <summary>
        /// 获取队首内存块（用于读取数据）。
        /// </summary>
        public byte[] First
        {
            get
            {
                if (this.bufferQueue.Count == 0)
                {
                    this.AddLast();
                }
                return this.bufferQueue.Peek();
            }
        }

        /// <summary>
        /// 获取队尾内存块（用于写入数据）。
        /// </summary>
        public byte[] Last
        {
            get
            {
                if (this.bufferQueue.Count == 0)
                {
                    this.AddLast();
                }
                return this.lastBuffer;
            }
        }

		/// <summary>
		/// 从CircularBuffer读到stream中
		/// </summary>
		/// <param name="stream"></param>
		/// <returns></returns>
		//public async ETTask ReadAsync(Stream stream)
	    //{
		//    long buffLength = this.Length;
		//	int sendSize = this.ChunkSize - this.FirstIndex;
		//    if (sendSize > buffLength)
		//    {
		//	    sendSize = (int)buffLength;
		//    }
		//	
		//    await stream.WriteAsync(this.First, this.FirstIndex, sendSize);
		//    
		//    this.FirstIndex += sendSize;
		//    if (this.FirstIndex == this.ChunkSize)
		//    {
		//	    this.FirstIndex = 0;
		//	    this.RemoveFirst();
		//    }
		//}

	    // 从CircularBuffer读到stream
	    public void Read(Stream stream, int count)
	    {
		    if (count > this.Length)
		    {
			    throw new Exception($"bufferList length < count, {Length} {count}");
		    }

		    int alreadyCopyCount = 0;
		    while (alreadyCopyCount < count)
		    {
			    int n = count - alreadyCopyCount;
			    if (ChunkSize - this.FirstIndex > n)
			    {
				    stream.Write(this.First, this.FirstIndex, n);
				    this.FirstIndex += n;
				    alreadyCopyCount += n;
			    }
			    else
			    {
				    stream.Write(this.First, this.FirstIndex, ChunkSize - this.FirstIndex);
				    alreadyCopyCount += ChunkSize - this.FirstIndex;
				    this.FirstIndex = 0;
				    this.RemoveFirst();
			    }
		    }
	    }
	    
	    // 从stream写入CircularBuffer
	    public void Write(Stream stream)
		{
			int count = (int)(stream.Length - stream.Position);
			
			int alreadyCopyCount = 0;
			while (alreadyCopyCount < count)
			{
				if (this.LastIndex == ChunkSize)
				{
					this.AddLast();
					this.LastIndex = 0;
				}

				int n = count - alreadyCopyCount;
				if (ChunkSize - this.LastIndex > n)
				{
					stream.Read(this.lastBuffer, this.LastIndex, n);
					this.LastIndex += count - alreadyCopyCount;
					alreadyCopyCount += n;
				}
				else
				{
					stream.Read(this.lastBuffer, this.LastIndex, ChunkSize - this.LastIndex);
					alreadyCopyCount += ChunkSize - this.LastIndex;
					this.LastIndex = ChunkSize;
				}
			}
		}
	    

	    /// <summary>
		///  从stream写入CircularBuffer
		/// </summary>
		/// <param name="stream"></param>
		/// <returns></returns>
		//public async ETTask<int> WriteAsync(Stream stream)
	    //{
		//    int size = this.ChunkSize - this.LastIndex;
		//    
		//    int n = await stream.ReadAsync(this.Last, this.LastIndex, size);
//
		//    if (n == 0)
		//    {
		//	    return 0;
		//    }
//
		//    this.LastIndex += n;
//
		//    if (this.LastIndex == this.ChunkSize)
		//    {
		//	    this.AddLast();
		//	    this.LastIndex = 0;
		//    }
//
		//    return n;
	    //}

	    // 把CircularBuffer中数据写入buffer
        public override int Read(byte[] buffer, int offset, int count)
        {
	        if (buffer.Length < offset + count)
	        {
		        throw new Exception($"bufferList length < coutn, buffer length: {buffer.Length} {offset} {count}");
	        }

	        long length = this.Length;
			if (length < count)
            {
	            count = (int)length;
            }

            int alreadyCopyCount = 0;
            while (alreadyCopyCount < count)
            {
                int n = count - alreadyCopyCount;
				if (ChunkSize - this.FirstIndex > n)
                {
                    Array.Copy(this.First, this.FirstIndex, buffer, alreadyCopyCount + offset, n);
                    this.FirstIndex += n;
                    alreadyCopyCount += n;
                }
                else
                {
                    Array.Copy(this.First, this.FirstIndex, buffer, alreadyCopyCount + offset, ChunkSize - this.FirstIndex);
                    alreadyCopyCount += ChunkSize - this.FirstIndex;
                    this.FirstIndex = 0;
                    this.RemoveFirst();
                }
            }

	        return count;
        }

	    // 把buffer写入CircularBuffer中
        public override void Write(byte[] buffer, int offset, int count)
        {
	        int alreadyCopyCount = 0;
            while (alreadyCopyCount < count)
            {
                if (this.LastIndex == ChunkSize)
                {
                    this.AddLast();
                    this.LastIndex = 0;
                }

                int n = count - alreadyCopyCount;
                if (ChunkSize - this.LastIndex > n)
                {
                    Array.Copy(buffer, alreadyCopyCount + offset, this.lastBuffer, this.LastIndex, n);
                    this.LastIndex += count - alreadyCopyCount;
                    alreadyCopyCount += n;
                }
                else
                {
                    Array.Copy(buffer, alreadyCopyCount + offset, this.lastBuffer, this.LastIndex, ChunkSize - this.LastIndex);
                    alreadyCopyCount += ChunkSize - this.LastIndex;
                    this.LastIndex = ChunkSize;
                }
            }
        }

	    public override void Flush()
	    {
		    throw new NotImplementedException();
		}

	    public override long Seek(long offset, SeekOrigin origin)
	    {
			throw new NotImplementedException();
	    }

	    public override void SetLength(long value)
	    {
		    throw new NotImplementedException();
		}

	    public override bool CanRead
	    {
		    get
		    {
			    return true;
		    }
	    }

	    public override bool CanSeek
	    {
		    get
		    {
			    return false;
		    }
	    }

	    public override bool CanWrite
	    {
		    get
		    {
			    return true;
		    }
	    }

	    public override long Position { get; set; }
    }
}
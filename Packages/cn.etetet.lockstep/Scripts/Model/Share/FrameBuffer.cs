using System;
using System.Collections.Generic;
using System.IO;

namespace ET
{
    /// <summary>
    /// 帧同步环形缓冲区，采用取模环形数组缓存最近 N 帧（默认 60 秒即 1200 帧）的全员输入 <see cref="OneFrameInputs"/>、世界序列化快照 <see cref="MemoryBuffer"/> 及状态哈希值，支持零 GC 复用与快速预测回滚
    /// </summary>
    public class FrameBuffer: Object
    {
        /// <summary>
        /// 当前环形缓冲区允许访问的最大帧号上限
        /// </summary>
        public int MaxFrame { get; private set; }

        /// <summary>
        /// 环形帧输入列表（按 <c>frame % Capacity</c> 索引）
        /// </summary>
        private readonly List<OneFrameInputs> frameInputs;

        /// <summary>
        /// 环形世界二进制快照缓冲区列表（按 <c>frame % Capacity</c> 索引）
        /// </summary>
        private readonly List<MemoryBuffer> snapshots;

        /// <summary>
        /// 环形帧世界状态哈希值列表（按 <c>frame % Capacity</c> 索引）
        /// </summary>
        private readonly List<long> hashs;

        /// <summary>
        /// 构造帧同步环形缓冲区并预分配所有帧的输入对象与快照内存流
        /// </summary>
        /// <param name="frame">初始帧号</param>
        /// <param name="capacity">环形缓冲区总帧数容量（默认 60 秒的帧数）</param>
        public FrameBuffer(int frame = 0, int capacity = LSConstValue.FrameCountPerSecond * 60)
        {
            this.MaxFrame = frame + LSConstValue.FrameCountPerSecond * 30;
            this.frameInputs = new List<OneFrameInputs>(capacity);
            this.snapshots = new List<MemoryBuffer>(capacity);
            this.hashs = new List<long>(capacity);
            
            for (int i = 0; i < this.snapshots.Capacity; ++i)
            {
                this.hashs.Add(0);
                this.frameInputs.Add(OneFrameInputs.Create());
                MemoryBuffer memoryBuffer = new(10240);
                memoryBuffer.SetLength(0);
                memoryBuffer.Seek(0, SeekOrigin.Begin);
                this.snapshots.Add(memoryBuffer);
            }
        }

        /// <summary>
        /// 记录指定帧的世界快照哈希值
        /// </summary>
        public void SetHash(int frame, long hash)
        {
            EnsureFrame(frame);
            this.hashs[frame % this.frameInputs.Capacity] = hash;
        }
        
        /// <summary>
        /// 获取指定帧的世界快照哈希值
        /// </summary>
        public long GetHash(int frame)
        {
            EnsureFrame(frame);
            return this.hashs[frame % this.frameInputs.Capacity];
        }

        /// <summary>
        /// 检查指定帧号是否处于合法可访问范围内（<c>0 &lt;= frame &lt;= MaxFrame</c>）
        /// </summary>
        public bool CheckFrame(int frame)
        {
            if (frame < 0)
            {
                return false;
            }

            if (frame > this.MaxFrame)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 断言帧号合法，越界时抛出异常
        /// </summary>
        private void EnsureFrame(int frame)
        {
            if (!CheckFrame(frame))
            {
                throw new Exception($"frame out: {frame}, maxframe: {this.MaxFrame}");
            }
        }
        
        /// <summary>
        /// 获取指定帧对应的全员输入容器 <see cref="OneFrameInputs"/>
        /// </summary>
        public OneFrameInputs FrameInputs(int frame)
        {
            EnsureFrame(frame);
            OneFrameInputs oneFrameInputs = this.frameInputs[frame % this.frameInputs.Capacity];
            return oneFrameInputs;
        }

        /// <summary>
        /// 向前推进环形缓冲区窗口上限 <see cref="MaxFrame"/> 并清空新滑入槽位的旧帧输入数据
        /// </summary>
        /// <param name="frame">当前正在处理的帧号</param>
        public void MoveForward(int frame)
        {
            if (this.MaxFrame - frame > LSConstValue.FrameCountPerSecond) // 至少留出1秒的空间
            {
                return;
            }
            
            ++this.MaxFrame;
            
            OneFrameInputs oneFrameInputs = this.FrameInputs(this.MaxFrame);
            oneFrameInputs.Inputs.Clear();
        }

        /// <summary>
        /// 获取指定帧对应的世界快照内存流 <see cref="MemoryBuffer"/>
        /// </summary>
        public MemoryBuffer Snapshot(int frame)
        {
            EnsureFrame(frame);
            MemoryBuffer memoryBuffer = this.snapshots[frame % this.snapshots.Capacity];
            return memoryBuffer;
        }
    }
}
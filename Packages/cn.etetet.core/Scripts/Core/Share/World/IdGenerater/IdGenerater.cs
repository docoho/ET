using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace ET
{
    /// <summary>
    /// 64 位持久化业务 ID（雪花算法变体）的位域拆解结构体。
    /// <para>
    /// 位布局（共 64 bit）：<c>[高 16 bit: Process 进程标识] + [中 28 bit: 自 2025 年起的秒数] + [低 20 bit: 每秒内自增序号]</c>。
    /// </para>
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IdStruct
    {
        /// <summary>高 16 位：进程与副本唯一编号（0 ~ 65535）</summary>
        public ushort Process;  // 16bit
        /// <summary>中 28 位：相对于 2025-01-01 00:00:00 UTC 的秒数（2^28 秒 ≈ 8.5 年）</summary>
        public uint Time;    // 28bit  可用大约8年
        /// <summary>低 20 位：同一秒内的自增序列号（单进程每秒最多可生成 2^20 = 1,048,575 个唯一 ID）</summary>
        public uint Value;   // 20bit

        /// <summary>
        /// 将三段字段打包合成为一个 64 位 <see cref="long"/> 整数。
        /// </summary>
        public long ToLong()
        {
            ulong result = 0;
            result |= this.Process;
            result <<= 28;
            result |= this.Time;
            result <<= 20;
            result |= this.Value;
            return (long) result;
        }

        public IdStruct(uint time, ushort process, uint value)
        {
            this.Process = process;
            this.Time = time;
            this.Value = value;
        }

        /// <summary>
        /// 将一个 64 位 <paramref name="id"/> 反向拆解还原出进程号、时间戳与自增序列号。
        /// </summary>
        public IdStruct(long id)
        {
            ulong result = (ulong) id;
            this.Value = (uint) (result & IdGenerater.Mask20bit);
            result >>= 20;
            this.Time = (uint) (result & IdGenerater.Mask28bit);
            result >>= 28;
            this.Process = (ushort) (result & IdGenerater.Mask16bit);
        }

        public override string ToString()
        {
            return $"process: {this.Process}, time: {this.Time}, value: {this.Value}";
        }
    }

    /// <summary>
    /// 分布式全局唯一 64 位业务 ID 生成器单例（Snowflake 雪花算法）。
    /// 用于生成实体的持久化 <see cref="Entity.Id"/>（如角色 ID、道具 ID、邮件 ID 等，保证跨进程、跨重启全局唯一）。
    /// </summary>
    [AllowInstance]
    public class IdGenerater: Singleton<IdGenerater>, ISingletonAwake
    {
        public const uint Mask32bit = 0xffffffff;
        public const uint Mask28bit = 0xfffffff;  // 28bit mask: 268,435,455
        public const uint Mask20bit = 0xfffff;    // 20bit mask: 1,048,575
        public const uint Mask16bit = 0xffff;
        
        private long epoch2025;
        
        private uint value;
        private uint second; // 秒
        
        public void Awake()
        {
            long epoch1970tick = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks / 10000;
            this.epoch2025 = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks / 10000 - epoch1970tick;
        }

        private uint TimeSince2025()
        {
            uint a = (uint)((TimeInfo.Instance.ClientNow() - this.epoch2025) / 1000);
            return a;
        }
        
        /// <summary>
        /// 线程安全地生成一个新的 64 位全局唯一持久化 ID（若单秒内生成超过 104 万个，会自动向下一秒借位避免重复）。
        /// </summary>
        public long GenerateId()
        {
            uint time = TimeSince2025();
            uint v = 0;
            uint s = 0;
            // 这里必须加锁
            lock (this)
            {
                if (time > this.second)
                {
                    this.value = 0;
                    this.second = time;
                }
                else
                {
                    ++this.value;
                    if (this.value == IdGenerater.Mask20bit)  
                    {
                        ++this.second; // 借用下一秒的id
                        this.value = 0;
                    }
                }
                s = this.second;
                v = this.value;
            }

            IdStruct idStruct = new(s, this.GetProcessReplicaIndex(), v);
            return idStruct.ToLong();
        }

        /// <summary>
        /// 获取当前进程及副本组合出的唯一 16 位进程标识（<c>Options.Process + Options.ReplicaIndex</c>）。
        /// </summary>
        public ushort GetProcessReplicaIndex()
        {
            // 因为改成了服务发现，支持一个进程多个副本，比如gate只需要配一个进程，可以支持多个,同一个Replica是同一个进程Id
            // 需要预留出来Replica数量的进程Id
            if (Options.Instance.Process > 50000)
            {
                throw new Exception($"Process is too large: {Options.Instance.Process}");
            }
            return (ushort)(Options.Instance.Process + Options.Instance.ReplicaIndex);
        }
    }
}

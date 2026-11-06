using System;
using System.Net;
using System.Runtime.InteropServices;
using MemoryPack;

namespace ET
{
    /// <summary>
    /// 网络端点地址结构体（包含 IP 字符串与端口号 Port），支持与 <see cref="IPEndPoint"/> 隐式互转及 MemoryPack 高效序列化。
    /// </summary>
    [MemoryPackable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public partial struct Address: IEquatable<Address>
    {
        /// <summary>IPv4 / IPv6 地址字符串</summary>
        [MemoryPackOrder(0)]
        public string IP;
        /// <summary>监听端口号</summary>
        [MemoryPackOrder(1)]
        public int Port;
        
        public bool Equals(Address other)
        {
            return this.IP == other.IP && this.Port == other.Port;
        }

        public override bool Equals(object obj)
        {
            return obj is Address other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.IP, this.Port);
        }
        
        public Address(string ip, int port)
        {
            this.IP = ip;
            this.Port = port;
        }

        public static bool operator ==(Address left, Address right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Address left, Address right)
        {
            return !(left == right);
        }

        public static implicit operator IPEndPoint(Address address)
        {
            return new IPEndPoint(IPAddress.Parse(address.IP), address.Port);
        }

        public static implicit operator Address(IPEndPoint endPoint)
        {
            return new Address(endPoint.Address.ToString(), endPoint.Port);
        }

        public override string ToString()
        {
            return MongoHelper.ToJson(this);
        }
    }
    
    /// <summary>
    /// 进程内 Actor 寻址标识结构体：由目标纤程 ID（<see cref="Fiber"/>）与目标实体实例 ID（<see cref="InstanceId"/>）组成。
    /// </summary>
    /// <remarks>
    /// 【初学者指南】
    /// - 当向一个 <see cref="Scene"/>（Fiber 根节点）发消息时，其默认 <c>InstanceId</c> 为 <c>1</c>，此时可以直接用 <c>new FiberInstanceId(fiberId)</c> 构造；
    /// - 当向某个具体 <see cref="Entity"/>（如挂载了 <c>MailBoxComponent</c> 的玩家 <c>Unit</c> 或 <c>GatePlayer</c>）发消息时，需要同时指定它所在的 <c>Fiber</c> 和它自身的 <c>InstanceId</c>。
    /// </remarks>
    [MemoryPackable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public partial struct FiberInstanceId: IEquatable<FiberInstanceId>
    {
        /// <summary>目标纤程的 64 位唯一编码 ID（可由 <see cref="FiberIdHelper"/> 编码场景类型、区服等语义）</summary>
        [MemoryPackOrder(0)]
        public long Fiber;
        /// <summary>目标实体的运行时唯一 <see cref="Entity.InstanceId"/>（若目标是 Fiber 根 Scene，则固定为 1）</summary>
        [MemoryPackOrder(1)]
        public long InstanceId;
        
        public bool Equals(FiberInstanceId other)
        {
            return this.Fiber == other.Fiber && this.InstanceId == other.InstanceId;
        }

        public override bool Equals(object obj)
        {
            return obj is FiberInstanceId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Fiber, this.InstanceId);
        }
        
        public FiberInstanceId(long fiber, long instanceId)
        {
            this.Fiber = fiber;
            this.InstanceId = instanceId;
        }
        
        /// <summary>
        /// 构造指向目标 <paramref name="fiber"/> 根场景（<c>InstanceId == 1</c>）的寻址标识。
        /// </summary>
        public FiberInstanceId(long fiber)
        {
            this.Fiber = fiber;
            this.InstanceId = 1;
        }

        public static bool operator ==(FiberInstanceId left, FiberInstanceId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FiberInstanceId left, FiberInstanceId right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return $"{this.Fiber}:{this.InstanceId}";
        }
    }
    
    /// <summary>
    /// 分布式全局 Actor 寻址完整地址（<see cref="ActorId"/>）。
    /// </summary>
    /// <remarks>
    /// 【初学者必读 —— ET 分布式 Actor 模型的核心】
    /// 在 ET10 的多进程分布式服务器集群中，如何从任意一台服务器把一条消息精确投递给另一台服务器上的某一个 <see cref="Entity"/>？
    /// 答案就是靠这个 <see cref="ActorId"/> 三元组：
    /// 1. <see cref="Address"/>（IP + Port）：找到目标实体所在的<b>物理服务器进程</b>（通过内网 <c>NetInner</c> 发送过去）；
    /// 2. <see cref="FiberInstanceId.Fiber"/>：目标进程收到后，通过 <c>Mailboxes</c> 找到目标<b>纤程（Fiber）</b>并投递到它的单线程队列；
    /// 3. <see cref="FiberInstanceId.InstanceId"/>：目标纤程在自己的 <c>Mailboxes</c> 字典中精确找到挂载了 <c>MailBoxComponent</c> 的<b>具体目标实体</b>！
    /// </remarks>
    [MemoryPackable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public partial struct ActorId: IEquatable<ActorId>
    {
        /// <summary>目标进程的网络物理地址（IP + 端口）</summary>
        [MemoryPackOrder(0)]
        public Address Address;
        
        /// <summary>目标进程内的纤程 ID 与实体 InstanceId 组合</summary>
        [MemoryPackOrder(1)]
        public FiberInstanceId FiberInstanceId;
        
        
        public ActorId(Address address, FiberInstanceId fiberInstanceId)
        {
            this.Address = address;
            this.FiberInstanceId = fiberInstanceId;
        }
        
        public bool Equals(ActorId other)
        {
            return this.Address == other.Address && this.FiberInstanceId == other.FiberInstanceId;
        }

        public override bool Equals(object obj)
        {
            return obj is ActorId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Address.GetHashCode(), this.FiberInstanceId.GetHashCode());
        }
        
        public static bool operator ==(ActorId left, ActorId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ActorId left, ActorId right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return MongoHelper.ToJson(this);
        }
    }
}

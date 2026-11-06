using System;
using System.IO;

namespace ET
{
    /// <summary>
    /// 网络消息封包（序列化 + 写入协议头）与解包（解析协议头 + 反序列化）核心辅助工具类。
    /// <remarks>
    /// 封包格式对比：
    /// - 外网（<see cref="ServiceType.Outer"/>）：<c>[2 字节 Opcode] + [MemoryPack 消息体]</c>
    /// - 内网（<see cref="ServiceType.Inner"/>）：<c>[16 字节 FiberInstanceId] + [2 字节 Opcode] + [MemoryPack 消息体]</c>
    /// </remarks>
    /// </summary>
    public static class MessageSerializeHelper
    {
        /// <summary>
        /// 将消息对象序列化为 MemoryPack 字节数组。
        /// </summary>
        public static byte[] Serialize(MessageObject message)
        {
            return MemoryPackHelper.Serialize(message);
        }

        /// <summary>
        /// 将消息对象序列化写入指定的 <see cref="MemoryBuffer"/> 中。
        /// </summary>
        public static void Serialize(MessageObject message, MemoryBuffer stream)
        {
            MemoryPackHelper.Serialize(message, stream);
        }
		
        /// <summary>
        /// 从对象池获取指定类型的消息实例，并从字节数组切片中反序列化填充其字段。
        /// </summary>
        public static MessageObject Deserialize(Type type, byte[] bytes, int index, int count)
        {
            object o = ObjectPool.Fetch(type);
            MemoryPackHelper.Deserialize(type, bytes, index, count, ref o);
            return o as MessageObject;
        }

        /// <summary>
        /// 从对象池获取指定类型的消息实例，并从 <see cref="MemoryBuffer"/> 中反序列化填充其字段。
        /// </summary>
        public static MessageObject Deserialize(Type type, MemoryBuffer stream)
        {
            object o = ObjectPool.Fetch(type);
            MemoryPackHelper.Deserialize(type, stream, ref o);
            return o as MessageObject;
        }
        
        /// <summary>
        /// 将消息的 2 字节 Opcode 及序列化后的消息体写入 <paramref name="stream"/>（支持通过 <paramref name="headOffset"/> 为内网消息预留 16 字节头部空间）。
        /// </summary>
        public static ushort MessageToStream(MemoryBuffer stream, MessageObject message, int headOffset = 0)
        {
            ushort opcode = OpcodeType.Instance.GetOpcode(message.GetType());
            stream.Seek(headOffset + Packet.OpcodeLength, SeekOrigin.Begin);
            stream.SetLength(headOffset + Packet.OpcodeLength);
            
            stream.GetBuffer().WriteTo(headOffset, opcode);
            
            MessageSerializeHelper.Serialize(message, stream);
            
            stream.Seek(0, SeekOrigin.Begin);
            return opcode;
        }
        
        /// <summary>
        /// 从网络服务的缓冲池中申请 <see cref="MemoryBuffer"/>，并根据内外网类型将消息（及内网 <paramref name="fiberInstanceId"/>）完整封包写入缓冲区。
        /// </summary>
        public static (ushort, MemoryBuffer) ToMemoryBuffer(AService service, FiberInstanceId fiberInstanceId, object message)
        {
            MemoryBuffer memoryBuffer = service.Fetch();
            ushort opcode = 0;
            switch (service.ServiceType)
            {
                case ServiceType.Inner:
                {
                    opcode = MessageToStream(memoryBuffer, (MessageObject)message, Packet.FiberInstanceIdLength);
                    memoryBuffer.GetBuffer().WriteTo(0, fiberInstanceId);
                    break;
                }
                case ServiceType.Outer:
                {
                    opcode = MessageToStream(memoryBuffer, (MessageObject)message);
                    break;
                }
            }
            
            return (opcode, memoryBuffer);
        }
        
        /// <summary>
        /// 根据内外网服务类型，从接收到的 <see cref="MemoryBuffer"/> 中解包出目标 <see cref="FiberInstanceId"/>（外网为 default）及反序列化后的消息对象。
        /// </summary>
        public static (FiberInstanceId, object) ToMessage(AService service, MemoryBuffer memoryStream)
        {
            object message = null;
            FiberInstanceId fiberInstanceId = default;
            switch (service.ServiceType)
            {
                case ServiceType.Outer:
                {
                    memoryStream.Seek(Packet.OpcodeLength, SeekOrigin.Begin);
                    ushort opcode = BitConverter.ToUInt16(memoryStream.GetBuffer(), 0);
                    Type type = OpcodeType.Instance.GetType(opcode);
                    message = Deserialize(type, memoryStream);
                    break;
                }
                case ServiceType.Inner:
                {
                    memoryStream.Seek(Packet.FiberInstanceIdLength + Packet.OpcodeLength, SeekOrigin.Begin);
                    byte[] buffer = memoryStream.GetBuffer();
                    fiberInstanceId.Fiber = BitConverter.ToInt64(buffer, Packet.FiberInstanceIdIndex);
                    fiberInstanceId.InstanceId = BitConverter.ToInt64(buffer, Packet.FiberInstanceIdIndex + 8);
                    ushort opcode = BitConverter.ToUInt16(buffer, Packet.FiberInstanceIdLength);
                    
                    Type type = OpcodeType.Instance.GetType(opcode);
                    message = Deserialize(type, memoryStream);
                    break;
                }
            }
            
            return (fiberInstanceId, message);
        }
    }
}

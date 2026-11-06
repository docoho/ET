using MemoryPack;

namespace ET
{
    /// <summary>
    /// 通用默认 RPC 响应消息（Opcode 为 <c>ushort.MaxValue</c> = 65535）。
    /// <remarks>
    /// 当某个 Actor / Location 请求不需要返回额外的自定义字段（只需告知调用方执行完成或返回 <see cref="Error"/> 错误码）时，统一使用此消息作为应答类型。
    /// </remarks>
    /// </summary>
    [Message(ushort.MaxValue)]
    [MemoryPackable]
    public partial class MessageResponse: MessageObject, IResponse
    {
        [MemoryPackOrder(1)]
        public int RpcId { get; set; }
        [MemoryPackOrder(2)]
        public int Error { get; set; }
        [MemoryPackOrder(3)]
        public string Message { get; set; }
    }
}
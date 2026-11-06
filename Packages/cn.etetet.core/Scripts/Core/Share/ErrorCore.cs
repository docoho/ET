namespace ET
{
    /// <summary>
    /// 框架核心层（Core）错误码定义。
    /// <remarks>
    /// 主要用于底层网络（KCP、TCP、WebSocket）连接、收发、拆包等异常状态标识。
    /// 注意：业务层自定义错误码应当在 110000 以上，以避免与系统 SocketError 及底层错误码冲突。
    /// </remarks>
    /// </summary>
    [UniqueId]
    internal static partial class ErrorCore
    {
        /// <summary>
        /// 操作成功（无错误）。
        /// </summary>
        public const int ERR_Success = 0;
        
        /// <summary>
        /// 自定义错误码起始基准值（110000 以上避免与系统 SocketError 冲突）。
        /// </summary>
        public const int ERR_MyErrorCode = 110000;
        
        /// <summary>
        /// KCP 发起连接超时。
        /// </summary>
        public const int ERR_KcpConnectTimeout = 100205;
        /// <summary>
        /// KCP 服务端接受连接超时。
        /// </summary>
        public const int ERR_KcpAcceptTimeout = 100206;
        /// <summary>
        /// KCP 读写空闲超时。
        /// </summary>
        public const int ERR_KcpReadWriteTimeout = 100207;
        /// <summary>
        /// 对端主动断开连接。
        /// </summary>
        public const int ERR_PeerDisconnect = 100208;
        /// <summary>
        /// Socket 当前无法发送数据。
        /// </summary>
        public const int ERR_SocketCantSend = 100209;
        /// <summary>
        /// 底层 Socket 发生一般性错误。
        /// </summary>
        public const int ERR_SocketError = 100210;
        /// <summary>
        /// KCP 等待发送队列积压过大（超出最大限制，通常意味着对端接收过慢或掉线）。
        /// </summary>
        public const int ERR_KcpWaitSendSizeTooLarge = 100211;
        /// <summary>
        /// KCP 实例创建失败。
        /// </summary>
        public const int ERR_KcpCreateError = 100212;
        /// <summary>
        /// 发送消息时未找到对应的 TCP 通道（TChannel）。
        /// </summary>
        public const int ERR_SendMessageNotFoundTChannel = 100213;
        /// <summary>
        /// TCP 通道（TChannel）接收数据异常。
        /// </summary>
        public const int ERR_TChannelRecvError = 100214;
        /// <summary>
        /// Socket 消息解析错误。
        /// </summary>
        public const int ERR_MessageSocketParserError = 100215;
        /// <summary>
        /// 未找到对应的 KCP 通道（KChannel）。
        /// </summary>
        public const int ERR_KcpNotFoundChannel = 100216;

        /// <summary>
        /// WebSocket 发送数据错误。
        /// </summary>
        public const int ERR_WebsocketSendError = 100217;
        /// <summary>
        /// WebSocket 对端重置连接。
        /// </summary>
        public const int ERR_WebsocketPeerReset = 100218;
        /// <summary>
        /// WebSocket 单条消息体积过大，超出接收缓冲区上限。
        /// </summary>
        public const int ERR_WebsocketMessageTooBig = 100219;
        /// <summary>
        /// WebSocket 接收数据错误。
        /// </summary>
        public const int ERR_WebsocketRecvError = 100220;
        
        /// <summary>
        /// KCP 读取的数据长度与预期不一致。
        /// </summary>
        public const int ERR_KcpReadNotSame = 100230;
        /// <summary>
        /// KCP 大包分片或合包错误。
        /// </summary>
        public const int ERR_KcpSplitError = 100231;
        /// <summary>
        /// KCP 大包分片数量超出限制或不一致。
        /// </summary>
        public const int ERR_KcpSplitCountError = 100232;

        /// <summary>
        /// TCP 粘包/半包解析器（PacketParser）解析失败。
        /// </summary>
        public const int ERR_PacketParserError = 110005;
        /// <summary>
        /// WebSocket 建立连接失败。
        /// </summary>
        public const int ERR_WebsocketConnectError = 110304;
        
        /// <summary>
        /// 错误码掩码：当错误码附带此偏移量时，表示该错误需要抛出 <see cref="RpcException"/> 异常；否则仅返回错误码而不抛异常。
        /// </summary>
        public const int ERR_WithException = 100000000;
        
        // 110000 以上，避免跟SocketError冲突
    }
}
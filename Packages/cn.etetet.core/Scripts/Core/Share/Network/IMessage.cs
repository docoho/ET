using MemoryPack;

namespace ET
{
    // 不需要返回消息
    /// <summary>
    /// 所有网络协议消息与进程内消息的顶层根接口（单向通知消息直接实现此接口，无需应答返回）。
    /// </summary>
    public interface IMessage
    {
    }

    /// <summary>
    /// RPC 请求消息接口（一问一答模式中的请求方消息，需携带 <see cref="RpcId"/> 以便将返回的 <see cref="IResponse"/> 与等待协程配对）。
    /// </summary>
    public interface IRequest: IMessage
    {
        /// <summary>
        /// 本次 RPC 调用在发送端会话内递增的唯一流水号。
        /// </summary>
        int RpcId
        {
            get;
            set;
        }
    }

    /// <summary>
    /// RPC 响应消息接口（一问一答模式中的应答方消息，通过回传相同的 <see cref="RpcId"/> 唤醒调用方的 <c>await</c>）。
    /// </summary>
    public interface IResponse: IMessage
    {
        /// <summary>
        /// 业务处理结果错误码（0 表示成功 <c>ErrorCore.ERR_Success</c>，非 0 表示业务错误或系统异常）。
        /// </summary>
        int Error
        {
            get;
            set;
        }

        /// <summary>
        /// 附加的错误描述或提示文本信息。
        /// </summary>
        string Message
        {
            get;
            set;
        }

        /// <summary>
        /// 对应请求消息（<see cref="IRequest"/>）的 RPC 流水号。
        /// </summary>
        int RpcId
        {
            get;
            set;
        }
    }
}
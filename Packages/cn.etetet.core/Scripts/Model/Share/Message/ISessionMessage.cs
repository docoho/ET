namespace ET
{
    /// <summary>
    /// 通过 <see cref="Session"/> 收发的外网单向消息标记接口。
    /// </summary>
    public interface ISessionMessage: IMessage
    {
    }
    
    /// <summary>
    /// 通过 <see cref="Session"/> 发送的外网 RPC 请求消息接口。
    /// </summary>
    public interface ISessionRequest: ISessionMessage, IRequest
    {
    }
    
    /// <summary>
    /// 通过 <see cref="Session"/> 返回的外网 RPC 响应消息接口。
    /// </summary>
    public interface ISessionResponse: ISessionMessage, IResponse
    {

    }
}
namespace ET
{
    /// <summary>
    /// 发往可迁移实体（ActorLocation）的单向消息接口（底层继承 <see cref="ILocationRequest"/> 以通过自动回复 <see cref="MessageResponse"/> 确认消息已送达目标实体）。
    /// </summary>
    public interface ILocationMessage: ILocationRequest
    {
    }

    /// <summary>
    /// 发往可迁移实体（ActorLocation）的 RPC 请求消息接口。
    /// </summary>
    public interface ILocationRequest: IRequest
    {
    }

    /// <summary>
    /// 可迁移实体（ActorLocation）返回的 RPC 响应消息接口。
    /// </summary>
    public interface ILocationResponse: IResponse
    {
    }
}
using System;

namespace ET
{
    /// <summary>
    /// Actor 消息处理器（Handler）统一接口。
    /// <remarks>
    /// 业务代码通常不直接实现此接口，而是继承自泛型抽象基类 <c>MessageHandler&lt;E, Message&gt;</c> 或 <c>MessageHandler&lt;E, Request, Response&gt;</c>。
    /// </remarks>
    /// </summary>
    public interface IMHandler
    {
        /// <summary>
        /// 处理投递到目标实体 <paramref name="entity"/> 的 Actor 消息 <paramref name="actorMessage"/>。
        /// </summary>
        /// <param name="entity">接收消息的 Actor 实体</param>
        /// <param name="fromFiber">发送方所在的全局 FiberId</param>
        /// <param name="actorMessage">收到的消息对象</param>
        ETTask Handle(Entity entity, long fromFiber, MessageObject actorMessage);
        /// <summary>
        /// 获取当前处理器负责监听的消息类型。
        /// </summary>
        Type GetRequestType();
        /// <summary>
        /// 获取当前处理器返回的 RPC 响应类型（若为单向通知消息则返回 null）。
        /// </summary>
        Type GetResponseType();
    }
}

using System;

namespace ET
{
    /// <summary>
    /// 基于 <see cref="Session"/> 的外网直连网络消息处理器统一接口。
    /// <remarks>
    /// 业务代码通常继承自泛型基类 <c>MessageSessionHandler&lt;Message&gt;</c> 或 <c>MessageSessionHandler&lt;Request, Response&gt;</c>。
    /// </remarks>
    /// </summary>
    public interface IMessageSessionHandler
    {
        /// <summary>
        /// 处理从指定 <paramref name="session"/> 接收到的网络消息 <paramref name="message"/>。
        /// </summary>
        void Handle(Session session, object message);
        /// <summary>
        /// 获取当前处理器负责监听的消息类型。
        /// </summary>
        Type GetMessageType();

        /// <summary>
        /// 获取当前处理器返回的响应消息类型（单向消息返回 null）。
        /// </summary>
        Type GetResponseType();
    }
}
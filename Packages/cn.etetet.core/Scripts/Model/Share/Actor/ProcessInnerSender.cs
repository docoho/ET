using System;
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 进程内跨 Fiber 消息收发组件，挂载在每个 Fiber 的根 <see cref="Scene"/> 上。
    /// <remarks>
    /// 核心职责：
    /// 1. 提供向同进程其他 Fiber 发送单向 Actor 消息（<c>Send</c>）、发起 RPC 请求（<c>Call</c>）以及回复 RPC 响应（<c>Reply</c>）的能力；
    /// 2. 在每帧 <c>Update</c> 中从全局 <see cref="MessageQueue"/> 拉取投递给当前 Fiber 的所有消息，分发给对应的 <see cref="MailBoxComponent"/> 或唤醒挂起的 RPC 回调 <see cref="requestCallback"/>。
    /// </remarks>
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ProcessInnerSender: Entity, IAwake, IDestroy, IUpdate
    {
        /// <summary>
        /// 进程内 Actor RPC 调用的默认超时时间（40 秒 = 40000 毫秒）。
        /// </summary>
        public const long TIMEOUT_TIME = 40 * 1000;
        
        /// <summary>
        /// 当前 Fiber 递增分配的 RPC 流水号。
        /// </summary>
        public int RpcId;

        /// <summary>
        /// 正在等待响应的进程内 RPC 请求回调字典（Key: RpcId, Value: <see cref="ProcessInnerMessageSenderStruct"/>）。
        /// </summary>
        public readonly Dictionary<int, ProcessInnerMessageSenderStruct> requestCallback = new();
        
        /// <summary>
        /// 每帧从 <see cref="MessageQueue"/> 批量拉取消息时复用的临时列表。
        /// </summary>
        public readonly List<MessageInfo> list = new();
    }
}
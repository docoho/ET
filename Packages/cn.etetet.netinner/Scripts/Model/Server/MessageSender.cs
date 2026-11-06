using System;
using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 业务场景（Scene）的 Actor 消息发送组件。
    /// <para>为当前 Fiber 提供统一的 <c>Send</c> 与 <c>Call</c> 接口，自动区分进程内直发与跨进程通过 <c>NetInner</c> 转发。</para>
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class MessageSender: Entity, IAwake
    {
        /// <summary>Actor RPC 默认超时时间（40 秒）。</summary>
        public const long TIMEOUT_TIME = 40 * 1000;

        /// <summary>RPC 请求回调等待字典。</summary>
        public readonly Dictionary<int, MessageSenderStruct> requestCallback = new();

        private EntityRef<ProcessInnerSender> processInnerSender;
        
        /// <summary>当前根场景上的进程内消息发送组件引用。</summary>
        public ProcessInnerSender ProcessInnerSender
        {
            get
            {
                return this.processInnerSender;
            }
            set
            {
                this.processInnerSender = value;
            }
        }
    }
}
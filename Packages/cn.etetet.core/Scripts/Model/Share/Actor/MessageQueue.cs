using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 进程内跨 Fiber 投递的消息信封结构体。
    /// <remarks>
    /// 注意：入队时 <see cref="FiberInstanceId"/> 中的 <c>Fiber</c> 字段被复用为记录【发送方 FiberId】（<c>fromFiber</c>），
    /// 而 <c>InstanceId</c> 字段则记录【接收方目标实体的 InstanceId】。
    /// </remarks>
    /// </summary>
    public struct MessageInfo
    {
        /// <summary>
        /// 包含发送方 FiberId 与接收方目标实体 InstanceId 的复合标识。
        /// </summary>
        public FiberInstanceId FiberInstanceId;
        /// <summary>
        /// 传递的消息对象引用（同进程内免序列化直接传引用，或按需深拷贝）。
        /// </summary>
        public MessageObject MessageObject;
    }
    
    /// <summary>
    /// 进程内跨 Fiber 无锁并发消息队列全局单例。
    /// <remarks>
    /// 每个 <see cref="Fiber"/> 在创建时通过 <see cref="AddQueue"/> 注册一个专属的 <see cref="ConcurrentQueue{MessageInfo}"/>；
    /// 同进程内不同 Fiber 之间发送 Actor 消息时，无需经过 Socket 网络栈，直接调用 <see cref="Send"/> 将消息入队到目标 Fiber 的并发队列中，
    /// 并由目标 Fiber 的 <see cref="ProcessInnerSender"/> 在每帧 <c>Update</c> 时调用 <see cref="Fetch"/> 批量取出处理。
    /// </remarks>
    /// </summary>
    [AllowInstance]
    public class MessageQueue: Singleton<MessageQueue>, ISingletonAwake
    {
        /// <summary>
        /// 目标 FiberId 到其专属接收并发队列的映射字典。
        /// </summary>
        private readonly ConcurrentDictionary<long, ConcurrentQueue<MessageInfo>> messages = new();
        
        public void Awake()
        {
        }

        /// <summary>
        /// 从发送方 <paramref name="fiber"/> 向目标 <paramref name="fiberInstanceId"/> 投递一条进程内消息。
        /// </summary>
        public bool Send(Fiber fiber, FiberInstanceId fiberInstanceId, MessageObject messageObject)
        {
            LogMsg.Instance.Send(fiber, messageObject);
            if (!this.messages.TryGetValue(fiberInstanceId.Fiber, out var queue))
            {
                return false;
            }
            queue.Enqueue(new MessageInfo() { FiberInstanceId = new FiberInstanceId(fiber.Id, fiberInstanceId.InstanceId), MessageObject = messageObject});
            return true;
        }
        
        /// <summary>
        /// 由目标 <paramref name="fiber"/> 调用，从其专属队列中最多取出 <paramref name="count"/> 条待处理消息存入 <paramref name="list"/>。
        /// </summary>
        public void Fetch(Fiber fiber, int count, List<MessageInfo> list)
        {
            if (!this.messages.TryGetValue(fiber.Id, out var queue))
            {
                return;
            }

            for (int i = 0; i < count; ++i)
            {
                if (!queue.TryDequeue(out MessageInfo message))
                {
                    break;
                }
                LogMsg.Instance.Recv(fiber, message.MessageObject);
                list.Add(message);
            }
        }

        /// <summary>
        /// 为新创建的 Fiber 注册专属的消息接收队列。
        /// </summary>
        public void AddQueue(long fiberId)
        {
            var queue = new ConcurrentQueue<MessageInfo>();
            this.messages[fiberId] = queue;
        }
        
        /// <summary>
        /// 移除已销毁 Fiber 的消息接收队列。
        /// </summary>
        public void RemoveQueue(long fiberId)
        {
            this.messages.TryRemove(fiberId, out _);
        }
    }
}

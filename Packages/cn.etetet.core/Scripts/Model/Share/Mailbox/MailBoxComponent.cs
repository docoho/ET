namespace ET
{
    /// <summary>
    /// <see cref="MailBoxComponent"/> 的生命周期与消息投递扩展系统。
    /// </summary>
    [EntitySystemOf(typeof(MailBoxComponent))]
    public static partial class MailBoxComponentSystem
    {
        /// <summary>
        /// 初始化邮箱组件时，记录父实体的 <c>InstanceId</c>，并将自身注册到当前 <see cref="Fiber.Mailboxes"/> 管理器中，使父实体具备接收跨纤程/跨进程 Actor 消息的能力。
        /// </summary>
        [EntitySystem]       
        private static void Awake(this MailBoxComponent self, int mailBoxType)
        {
            Fiber fiber = self.Fiber();
            self.MailBoxType = mailBoxType;
            self.ParentInstanceId = self.Parent.InstanceId;
            fiber.Mailboxes.Add(self);
        }
        
        /// <summary>
        /// 邮箱组件销毁时，从当前 <see cref="Fiber.Mailboxes"/> 中注销父实体的路由记录。
        /// </summary>
        [EntitySystem]
        private static void Destroy(this MailBoxComponent self)
        {
            self.Fiber().Mailboxes.Remove(self.ParentInstanceId);
        }

        // 加到mailbox
        /// <summary>
        /// 向当前邮箱投递一条收到的 Actor 消息，并根据 <see cref="MailBoxComponent.MailBoxType"/> 通过 <see cref="EventSystem"/> 分发给对应的邮箱处理器（如 <c>MailBoxType_UnOrderedMessageHandler</c> 或有序队列处理器）。
        /// </summary>
        public static void Add(this MailBoxComponent self, long fromFiber, MessageObject messageObject)
        {
            // 根据mailboxType进行分发处理
            EventSystem.Instance.Invoke(self.MailBoxType, new MailBoxInvoker() {MailBoxComponent = self, MessageObject = messageObject, FromFiber = fromFiber});
        }
    }

    /// <summary>
    /// 邮箱消息触发时传递给 <c>AInvokeHandler&lt;MailBoxInvoker&gt;</c> 的上下文参数结构体。
    /// </summary>
    public struct MailBoxInvoker
    {
        /// <summary>
        /// 发送方所在的全局 FiberId（用于回传 RPC 响应）。
        /// </summary>
        public long FromFiber { get; set; }
        /// <summary>
        /// 接收到的消息对象。
        /// </summary>
        public MessageObject MessageObject { get; set; }
        /// <summary>
        /// 目标实体挂载的 <see cref="MailBoxComponent"/> 安全引用。
        /// </summary>
        public EntityRef<MailBoxComponent> MailBoxComponent { get; set; }
    }
    
    /// <summary>
    /// 挂上这个组件表示该Entity是一个Actor,接收的消息将会队列处理
    /// </summary>
    [ComponentOf]
    public class MailBoxComponent: Entity, IAwake<int>, IDestroy
    {
        /// <summary>
        /// 挂载此邮箱的父实体（即 Actor 宿主实体）的 <see cref="Entity.InstanceId"/>。
        /// </summary>
        public long ParentInstanceId { get; set; }
        // Mailbox的类型
        /// <summary>
        /// 邮箱的消息处理策略类型（参见 <see cref="ET.MailBoxType"/>，如无序并发、有序队列、网关转发等）。
        /// </summary>
        public int MailBoxType { get; set; }
    }
}

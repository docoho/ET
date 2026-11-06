namespace ET
{
    /// <summary>
    /// 无序邮箱消息处理器（對應 <see cref="MailBoxType.UnOrderedMessage"/>）。
    /// 当实体挂载的 <see cref="MailBoxComponent"/> 类型为无序消息时，收到的消息不会经过协程锁排队等待，
    /// 而是立即通过 <see cref="MessageDispatcher"/> 并发分发给对应的 Actor 消息处理器执行。
    /// 适用于无需严格保序、或者允许并发处理的实体消息场景。
    /// </summary>
    [Invoke(MailBoxType.UnOrderedMessage)]
    public class MailBoxType_UnOrderedMessageHandler: AInvokeHandler<MailBoxInvoker>
    {
        /// <summary>
        /// 处理邮箱投递过来的消息事件，直接转发给全局消息分发器。
        /// </summary>
        /// <param name="args">包含目标邮箱组件、来源纤程 ID 及消息对象的调用参数。</param>
        public override void Handle(MailBoxInvoker args)
        {
            MailBoxComponent mailBoxComponent = args.MailBoxComponent;
            
            MessageObject messageObject = args.MessageObject;
            
            // 将消息直接分发给邮箱所属的父实体（Actor）处理
            MessageDispatcher.Instance.Handle(mailBoxComponent.Parent, args.FromFiber, messageObject);
        }
    }
}
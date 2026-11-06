namespace ET.Server
{
    /// <summary>
    /// 有序邮箱（<see cref="MailBoxType.OrderedMessage"/>）消息分发处理器。
    /// 通过按实体 ID 加协程锁（<see cref="CoroutineLockType.Mailbox"/>）保证发往同一实体（如玩家 Unit）的 ActorLocation 消息严格按到达顺序串行执行，避免并发重入产生竞态。
    /// </summary>
    [Invoke(MailBoxType.OrderedMessage)]
    public class MailBoxType_OrderedMessageHandler: AInvokeHandler<MailBoxInvoker>
    {
        public override void Handle(MailBoxInvoker args)
        {
            HandleInner(args).Coroutine();
        }

        /// <summary>
        /// 在 Mailbox 协程锁保护下串行分发消息；若等待锁期间目标实体已被销毁（例如已经跨服迁走），且消息为请求，则自动回复 <see cref="ErrorCode.ERR_NotFoundActor"/> 触发发送方重试。
        /// </summary>
        private static async ETTask HandleInner(MailBoxInvoker args)
        {
            MailBoxComponent mailBoxComponent = args.MailBoxComponent;
            
            MessageObject messageObject = args.MessageObject;

            Fiber fiber = mailBoxComponent.Fiber();
            if (fiber.IsDisposed)
            {
                return;
            }

            EntityRef<MailBoxComponent> mailBoxComponentRef = mailBoxComponent;
            using EntityRef<CoroutineLock> coroutineLockRef = await fiber.Root.CoroutineLockComponent.Wait(CoroutineLockType.Mailbox, mailBoxComponent.Parent.Id, 5000);
            
            mailBoxComponent = mailBoxComponentRef;
            if (mailBoxComponent == null)
            {
                if (messageObject is IRequest request)
                {
                    IResponse resp = MessageHelper.CreateResponse(request.GetType(), request.RpcId, ErrorCode.ERR_NotFoundActor);
                    fiber.Root.GetComponent<ProcessInnerSender>().Reply(args.FromFiber, resp);
                }
                return;
            }
            await MessageDispatcher.Instance.HandleAsync(mailBoxComponent.Parent, args.FromFiber, messageObject);
        }
    }
}

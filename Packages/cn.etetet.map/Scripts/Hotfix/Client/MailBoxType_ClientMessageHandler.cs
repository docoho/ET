namespace ET.Client
{
    /// <summary>
    /// 客户端邮箱消息分发处理器（<see cref="MailBoxType.ClientMessage"/>）。
    /// <remarks>
    /// 若收到的消息实现了 <see cref="ICurrentMessage"/>，则自动转发到当前活动地图子场景（<see cref="CurrentScenesComponent.Scene"/>）的邮箱中处理；
    /// 否则直接在客户端根 Scene 上通过 <see cref="MessageDispatcher"/> 分发。
    /// </remarks>
    /// </summary>
    [Invoke(MailBoxType.ClientMessage)]
    public class MailBoxType_ClientMessageHandler: AInvokeHandler<MailBoxInvoker>
    {
        /// <summary>
        /// 处理客户端邮箱收到的网络或进程内消息。
        /// </summary>
        public override void Handle(MailBoxInvoker args)
        {
            MailBoxComponent mailBoxComponent = args.MailBoxComponent;
            MessageObject messageObject = args.MessageObject;

            if (messageObject is ICurrentMessage)
            {
                Scene clientScene = mailBoxComponent.Parent as Scene;
                Scene currentScene = clientScene?.GetComponent<CurrentScenesComponent>().Scene;
                if (currentScene == null)
                {
                    Log.Warning($"current scene mailbox not found: {messageObject}");
                    return;
                }

                MailBoxComponent currentMailBoxComponent = currentScene.GetComponent<MailBoxComponent>();
                currentMailBoxComponent.Add(args.FromFiber, messageObject);
                return;
            }

            MessageDispatcher.Instance.Handle(mailBoxComponent.Parent, args.FromFiber, messageObject);
        }
    }
}

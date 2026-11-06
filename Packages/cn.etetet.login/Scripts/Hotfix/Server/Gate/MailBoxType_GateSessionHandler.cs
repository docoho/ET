namespace ET.Server
{
    /// <summary>
    /// 网关会话邮箱处理器（MailBoxType.GateSession）：将内网发往 PlayerSessionComponent Actor 的消息直接通过外网 Session 转发给客户端。
    /// </summary>
    [Invoke(MailBoxType.GateSession)]
    public class MailBoxType_GateSessionHandler: AInvokeHandler<MailBoxInvoker>
    {
        public override void Handle(MailBoxInvoker args)
        {
            MailBoxComponent mailBoxComponent = args.MailBoxComponent;
            
            // 这里messageObject要发送出去，不能回收
            IMessage messageObject = args.MessageObject;
            
            if (mailBoxComponent.Parent is PlayerSessionComponent playerSessionComponent)
            {
                playerSessionComponent.Session?.Send(messageObject);
            }
        }
    }
}
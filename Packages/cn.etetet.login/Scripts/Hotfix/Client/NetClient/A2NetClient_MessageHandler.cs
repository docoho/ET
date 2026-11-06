namespace ET.Client
{
    /// <summary>
    /// NetClient 独立网络纤程处理主纤程委托发送的单向消息（A2NetClient_Message），通过网关 Session 发送到服务器。
    /// </summary>
    [MessageHandler(SceneType.NetClient)]
    public class A2NetClient_MessageHandler: MessageHandler<Scene, A2NetClient_Message>
    {
        protected override async ETTask Run(Scene root, A2NetClient_Message message)
        {
            root.GetComponent<SessionComponent>().Session.Send(message.MessageObject);
            await ETTask.CompletedTask;
        }
    }
}
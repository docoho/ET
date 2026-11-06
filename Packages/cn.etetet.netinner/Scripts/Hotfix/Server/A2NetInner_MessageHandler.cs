using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 本进程其他 Fiber 发往 <c>NetInner</c> 纤程的跨进程单向 Actor 消息处理器。
    /// <para>由 <see cref="MessageSenderSystem.Send"/> 在检测到目标 <see cref="ActorId"/> 位于远端进程时投递，本处理器在 <c>NetInner</c> 纤程内调用 <see cref="ProcessOuterSender"/> 通过内网 Socket 发送出去。</para>
    /// </summary>
    [MessageHandler(SceneType.NetInner)]
    public class A2NetInner_MessageHandler: MessageHandler<Scene, A2NetInner_Message>
    {
        protected override async ETTask Run(Scene root, A2NetInner_Message innerMessage)
        {
            root.GetComponent<ProcessOuterSender>().Send(innerMessage.ActorId, innerMessage.MessageObject);
            await ETTask.CompletedTask;
        }
    }
}
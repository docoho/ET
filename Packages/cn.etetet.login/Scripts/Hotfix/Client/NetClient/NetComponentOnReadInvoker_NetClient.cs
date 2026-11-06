using System;

namespace ET.Client
{
    /// <summary>
    /// 客户端网络纤程（NetClient）收到服务器网络包后的读取分发器：
    /// RPC 响应直接唤醒等待的 Session，普通下行消息通过 ProcessInnerSender 投递给主纤程（ParentFiberId）处理。
    /// </summary>
    [Invoke(SceneType.NetClient)]
    public class NetComponentOnReadInvoker_NetClient: AInvokeHandler<NetComponentOnRead>
    {
        public override void Handle(NetComponentOnRead args)
        {
            Session session = args.Session;
            object message = args.Message;
            Fiber fiber = session.Fiber();
            
            // 根据消息接口判断是不是Actor消息，不同的接口做不同的处理,比如需要转发给Chat Scene，可以做一个IChatMessage接口
            switch (message)
            {
                case IResponse response:
                {
                    session.OnResponse(response);
                    break;
                }
                case ISessionMessage:
                {
                    MessageSessionDispatcher.Instance.Handle(session, message);
                    break;
                }
                case IMessage iActorMessage:
                {
                    // 扔到Main纤程队列中
                    long parentFiberId = fiber.Root.GetComponent<FiberParentComponent>().ParentFiberId;
                    fiber.Root.GetComponent<ProcessInnerSender>().Send(new FiberInstanceId(parentFiberId), iActorMessage);
                    break;
                }
                default:
                {
                    throw new Exception($"not found handler: {message}");
                }
            }
        }
    }
}

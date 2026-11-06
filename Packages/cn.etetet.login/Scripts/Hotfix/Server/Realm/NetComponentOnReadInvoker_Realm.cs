using System;

namespace ET.Server
{
    /// <summary>
    /// Realm 服外网消息读取分发器：进行消息频率统计限流校验后，将 ISessionMessage 分发给对应的处理器。
    /// </summary>
    [Invoke(SceneType.Realm)]
    public class NetComponentOnReadInvoker_Realm: AInvokeHandler<NetComponentOnRead>
    {
        public override void Handle(NetComponentOnRead args)
        {
            Session session = args.Session;
            object message = args.Message;
            
            // 防刷频校验：单种消息超过频次阈值时直接断开连接
            MessageStatisticsComponent messageStatisticsComponent = session.GetComponent<MessageStatisticsComponent>();
            if (!messageStatisticsComponent.Check(message.GetType(), 10))
            {
                session.Error = ErrorCode.ERR_MessageCountTooMany;
                session.Dispose();
                return;
            }
            
            // 根据消息接口判断是不是Actor消息，不同的接口做不同的处理,比如需要转发给Chat Scene，可以做一个IChatMessage接口
            switch (message)
            {
                case ISessionMessage:
                {
                    MessageSessionDispatcher.Instance.Handle(session, message);
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
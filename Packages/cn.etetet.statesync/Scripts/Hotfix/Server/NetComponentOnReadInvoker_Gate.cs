using System;

namespace ET.Server
{
    /// <summary>
    /// 状态同步模式下网关（Gate）收到客户端网络数据包（<see cref="NetComponentOnRead"/>）的消息分发处理器
    /// </summary>
    [Invoke(SceneType.Gate)]
    public class NetComponentOnReadInvoker_Gate: AInvokeHandler<NetComponentOnRead>
    {
        /// <summary>
        /// 异步处理网关收到的数据包
        /// </summary>
        /// <param name="args">网络读取事件参数</param>
        public override void Handle(NetComponentOnRead args)
        {
            HandleAsync(args).Coroutine();
        }

        /// <summary>
        /// 执行客户端消息频率防刷检测（单类消息每秒上限 20 条），并根据消息接口类型分发至本地会话处理器或转发至玩家所在 Map 的 Unit Actor
        /// </summary>
        /// <param name="args">网络读取事件参数</param>
        private async ETTask HandleAsync(NetComponentOnRead args)
        {
            Session session = args.Session;
            EntityRef<Session> sessionRef = session;
            object message = args.Message;
            Scene root = session.Root();

            MessageStatisticsComponent messageStatisticsComponent = session.GetComponent<MessageStatisticsComponent>();
            if (!messageStatisticsComponent.Check(message.GetType(), 20))
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
                case ILocationMessage actorLocationMessage:
                {
                    long unitId = session.GetComponent<SessionPlayerComponent>().Player.Id;
                    root.GetComponent<MessageLocationSenderComponent>().Get(LocationType.Unit).Send(unitId, actorLocationMessage);
                    break;
                }
                case ILocationRequest actorLocationRequest: // gate session收到actor rpc消息，先向actor 发送rpc请求，再将请求结果返回客户端
                {
                    long unitId = session.GetComponent<SessionPlayerComponent>().Player.Id;
                    int rpcId = actorLocationRequest.RpcId; // 这里要保存客户端的rpcId
                    IResponse iResponse = await root.GetComponent<MessageLocationSenderComponent>().Get(LocationType.Unit).Call(unitId, actorLocationRequest);
                    
                    iResponse.RpcId = rpcId;
                    // session可能已经断开了，所以这里需要判断
                    session = sessionRef;
                    if (session != null)
                    {
                        session.Send(iResponse);
                    }
                    break;
                }
                case IRequest actorRequest:  // 分发IActorRequest消息，目前没有用到，需要的自己添加
                {
                    break;
                }
                case IMessage actorMessage:  // 分发IActorMessage消息，目前没有用到，需要的自己添加
                {
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
using System.Net;

namespace ET.Server
{
    /// <summary>
    /// 帧同步匹配服务纤程（<see cref="SceneType.Match"/>）初始化处理器
    /// </summary>
    [Invoke(SceneType.Match)]
    public class FiberInit_Match: AInvokeHandler<FiberInit, ETTask>
    {
        /// <summary>
        /// 为匹配服根场景挂载消息收发组件以及核心匹配管理组件 <see cref="MatchComponent"/>
        /// </summary>
        /// <param name="fiberInit">纤程初始化参数</param>
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Scene root = fiberInit.Fiber.Root;
            root.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            root.AddComponent<ProcessInnerSender>();
            root.AddComponent<MessageSender>();
            root.AddComponent<MatchComponent>();
            root.AddComponent<LocationProxyComponent>();
            root.AddComponent<MessageLocationSenderComponent>();

            await ETTask.CompletedTask;
        }
    }
}
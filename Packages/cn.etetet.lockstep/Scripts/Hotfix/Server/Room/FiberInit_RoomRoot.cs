using System.Net;

namespace ET.Server
{
    /// <summary>
    /// 帧同步单局房间根纤程（<see cref="SceneType.RoomRoot"/>）初始化处理器，每个对局房间独占一个独立纤程
    /// </summary>
    [Invoke((long)SceneType.RoomRoot)]
    public class FiberInit_RoomRoot: AInvokeHandler<FiberInit, ETTask>
    {
        /// <summary>
        /// 为房间纤程根场景挂载消息邮箱、定时器、协程锁及 Actor/Location 通信组件
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
            root.AddComponent<LocationProxyComponent>();
            root.AddComponent<MessageLocationSenderComponent>();

            await ETTask.CompletedTask;
        }
    }
}
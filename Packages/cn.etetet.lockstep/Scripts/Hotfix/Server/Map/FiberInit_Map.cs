using System.Net;

namespace ET.Server
{
    /// <summary>
    /// 帧同步模式下 Map 场景纤程（<see cref="SceneType.Map"/>）初始化处理器，负责挂载 <see cref="RoomManagerComponent"/> 管理房间创建
    /// </summary>
    [Invoke(SceneType.Map)]
    public class FiberInit_Map: AInvokeHandler<FiberInit, ETTask>
    {
        /// <summary>
        /// 初始化帧同步 Map 纤程根场景及核心组件
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
            root.AddComponent<RoomManagerComponent>();
            root.AddComponent<LocationProxyComponent>();
            root.AddComponent<MessageLocationSenderComponent>();

            await ETTask.CompletedTask;
        }
    }
}
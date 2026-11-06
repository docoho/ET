namespace ET.Client
{
    /// <summary>
    /// 客户端专用网络纤程（SceneType.NetClient）初始化回调：在独立线程池纤程中负责底层网络收发与路由重连，避免阻塞主线程。
    /// </summary>
    [Invoke(SceneType.NetClient)]
    public class FiberInit_NetClient: AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Scene root = fiberInit.Fiber.Root;
            root.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            root.AddComponent<ProcessInnerSender>();
            root.AddComponent<FiberParentComponent>();
            await ETTask.CompletedTask;
        }
    }
}
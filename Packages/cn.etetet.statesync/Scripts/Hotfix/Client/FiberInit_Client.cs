namespace ET.Client
{
    /// <summary>
    /// 状态同步客户端业务纤程（<see cref="SceneType.Client"/>）初始化处理器
    /// </summary>
    [Invoke(SceneType.Client)]
    public class FiberInit_Client: AInvokeHandler<FiberInit, ETTask>
    {
        /// <summary>
        /// 为客户端根场景挂载消息邮箱、定时器、协程锁、玩家/子场景管理、任务与背包等核心组件，并发布 <see cref="AppStartInitFinish"/> 事件触发登录界面创建
        /// </summary>
        /// <param name="fiberInit">纤程初始化参数</param>
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Scene root = fiberInit.Fiber.Root;
            
            EntityRef<Scene> rootRef = root;
            root.AddComponent<MailBoxComponent, int>(MailBoxType.ClientMessage);
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            root.AddComponent<ProcessInnerSender>();
            root.AddComponent<PlayerComponent>();
            root.AddComponent<CurrentScenesComponent>();
            root.AddComponent<ObjectWait>();
            root.AddComponent<QuestComponent>();
            root.AddComponent<ItemComponent>();
            
            root = rootRef;
            await EventSystem.Instance.PublishAsync(root, new AppStartInitFinish());
        }
    }
}

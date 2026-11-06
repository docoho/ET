namespace ET
{
    /// <summary>
    /// 状态同步双端共享初始化事件处理器（<see cref="EntryEvent1"/>）
    /// </summary>
    [Event(SceneType.All)]
    public class EntryEvent1_InitShare: AEvent<Scene, EntryEvent1>
    {
        /// <summary>
        /// 为主纤程根场景挂载定时器、协程锁、对象等待器、无序消息邮箱及进程内消息发送组件
        /// </summary>
        /// <param name="root">主纤程根场景</param>
        /// <param name="args">共享初始化事件参数</param>
        protected override async ETTask Run(Scene root, EntryEvent1 args)
        {
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            root.AddComponent<ObjectWait>();
            root.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);
            root.AddComponent<ProcessInnerSender>();

            await ETTask.CompletedTask;
        }
    }
}
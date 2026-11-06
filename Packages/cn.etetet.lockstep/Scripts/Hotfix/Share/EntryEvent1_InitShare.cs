namespace ET
{
    /// <summary>
    /// 帧同步双端共享启动事件处理器（<see cref="EntryEvent1"/>），负责在主纤程根场景上挂载基础通用组件
    /// </summary>
    [Event(SceneType.LockStep)]
    public class EntryEvent1_InitShare: AEvent<Scene, EntryEvent1>
    {
        /// <summary>
        /// 挂载定时器、协程锁、对象等待器、无序消息邮箱及进程内消息发送器
        /// </summary>
        /// <param name="root">主纤程根场景</param>
        /// <param name="args">共享启动事件参数</param>
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
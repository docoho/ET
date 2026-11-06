namespace ET.Client
{
    /// <summary>
    /// 状态同步客户端表现层初始化事件处理器（<see cref="EntryEvent3"/>）
    /// </summary>
    [Event(SceneType.StateSync)]
    public class EntryEvent3_InitClient : AEvent<Scene, EntryEvent3>
    {
        /// <summary>
        /// 在主线程（<see cref="SchedulerType.Parent"/>）创建客户端业务主纤程（<see cref="SceneType.Client"/>）
        /// </summary>
        /// <param name="root">主纤程根场景</param>
        /// <param name="args">表现层初始化事件参数</param>
        protected override async ETTask Run(Scene root, EntryEvent3 args)
        {
            Fiber fiber = root.Fiber;
            await fiber.CreateFiber(SchedulerType.Parent, IdGenerater.Instance.GenerateId(), SceneType.Client, "Client");
        }
    }
}
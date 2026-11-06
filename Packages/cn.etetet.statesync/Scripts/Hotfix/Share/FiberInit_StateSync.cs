using System;

namespace ET
{
    /// <summary>
    /// 状态同步主纤程（<see cref="SceneType.StateSync"/>）初始化处理器，按顺序派发共享层、服务端层、客户端表现层三级启动事件
    /// </summary>
    [Invoke(SceneType.StateSync)]
    public class FiberInit_StateSync: AInvokeHandler<FiberInit, ETTask>
    {
        /// <summary>
        /// 依次发布 <see cref="EntryEvent1"/>、<see cref="EntryEvent2"/>、<see cref="EntryEvent3"/>
        /// </summary>
        /// <param name="fiberInit">纤程初始化参数</param>
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Fiber fiber = fiberInit.Fiber;
            Scene root = fiber.Root;
            
            EntityRef<Scene> rootRef = root;
            await EventSystem.Instance.PublishAsync(root, new EntryEvent1());
            root = rootRef;
            await EventSystem.Instance.PublishAsync(root, new EntryEvent2());
            root = rootRef;
            await EventSystem.Instance.PublishAsync(root, new EntryEvent3());
        }
    }
}
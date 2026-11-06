namespace ET.Client
{
    /// <summary>
    /// 子纤程记录父纤程 ID 的组件：挂载在 NetClient 纤程的 Root Scene 上，用于将收到的服务器消息转发回主纤程（Main Fiber）。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class FiberParentComponent: Entity, IAwake
    {
        /// <summary>创建该纤程的父纤程 ID（通常为主线程 Main 纤程）</summary>
        public long ParentFiberId { get; set; }
    }
}

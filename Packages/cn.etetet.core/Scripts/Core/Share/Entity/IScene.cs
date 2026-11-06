namespace ET
{
    /// <summary>
    /// 场景抽象接口。任何挂载在实体树上的 <see cref="Entity"/> 都通过 <see cref="Entity.IScene"/> 指向一个 <see cref="IScene"/> 实例（如 <see cref="Scene"/> 或帧同步的 <c>LSWorld</c>）。
    /// </summary>
    public interface IScene
    {
        /// <summary>当前场景所属的纤程调度器</summary>
        Fiber Fiber { get; set; }
        /// <summary>当前场景的类型标识（决定事件分发与消息处理范围）</summary>
        int SceneType { get; set; }
    }
}
namespace ET
{
    /// <summary>
    /// Buff 周期性触发（Tick）与 AI 协程状态管理组件，维护定时器 ID 以及当前正在运行的异步协程节点状态。
    /// </summary>
    [ComponentOf(typeof(Buff))]
    public class BuffTickComponent: Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 周期性触发定时器 ID。
        /// </summary>
        public long TimerId;
        
        /// <summary>
        /// 是否作为独占 AI 压入 Unit 的 BTCoroutineComponent 栈中。
        /// </summary>
        public bool Override;
        
        /// <summary>
        /// 当前运行的异步行为树节点（BTCoroutine）取消令牌。
        /// </summary>
        public ETCancellationToken CancellationToken;
        /// <summary>
        /// 当前正在运行的 BTCoroutine 节点 ID。
        /// </summary>
        public int Current;
        /// <summary>
        /// 当前处理器实例的 HashCode，用于检测热重载后自动重启协程。
        /// </summary>
        public int HashCode;
        /// <summary>
        /// 宿主 Unit 的实体 ID。
        /// </summary>
        public long UnitId;
    }
}
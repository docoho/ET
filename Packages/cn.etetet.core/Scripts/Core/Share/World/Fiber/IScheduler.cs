using System;

namespace ET
{
    /// <summary>
    /// <see cref="Fiber"/> 纤程调度器内部抽象接口（实现类包括 <see cref="MainThreadScheduler"/>、<see cref="ThreadScheduler"/>、<see cref="ThreadPoolScheduler"/> 以及作为父节点的 <see cref="Fiber"/> 自身）。
    /// </summary>
    internal interface IScheduler
    {
        /// <summary>将纤程加入当前调度器的执行队列</summary>
        void AddToScheduler(Fiber fiber);
        /// <summary>停止并清理调度器</summary>
        void Dispose();
    }
}
using System;
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 单个具体 <c>(coroutineLockType, key)</c> 对应的协程等待队列实体（对象池复用）。
    /// 当该 key 下所有等待协程都执行完毕且队列为空时，会被父节点 <see cref="CoroutineLockQueueType"/> 自动回收。
    /// </summary>
    [ChildOf(typeof(CoroutineLockQueueType))]
    public partial class CoroutineLockQueue: Entity, IAwake<long>, IDestroy, IPool
    {
        /// <summary>所属的协程锁类型编号</summary>
        internal long type;

        /// <summary>允许同时持锁运行的最大并发协程数（默认为 1）</summary>
        internal int maxConcurrency { get; set; }

        /// <summary>当前正在持锁运行中的协程数量</summary>
        internal int runningCount;

        /// <summary>正在排队等待获取锁的 <see cref="ETTask{T}"/> 队列</summary>
        internal Queue<ETTask<EntityRef<CoroutineLock>>> queue = new();

        void IPool.Clear()
        {
            this.type = 0;
            this.maxConcurrency = 0;
            this.runningCount = 0;
            this.queue.Clear();
        }

        /// <summary>
        /// 当前仍在排队等待中的协程数量。
        /// </summary>
        internal int Count
        {
            get
            {
                return this.queue.Count;
            }
        }
    }
}

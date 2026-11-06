using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace ET
{
    /// <summary>
    /// 主线程纤程调度器（对应 <see cref="SchedulerType.Main"/>）。
    /// 由进程主循环或 Unity 主线程的 <c>Update</c> / <c>LateUpdate</c> 驱动其中注册的所有 Fiber。
    /// </summary>
    internal class MainThreadScheduler: IScheduler
    {
        private readonly ConcurrentQueue<Fiber> fiberQueue = new();
        private readonly ConcurrentQueue<Fiber> addQueue = new();

        private readonly int threadId = Environment.CurrentManagedThreadId;

        public void Dispose()
        {
            this.addQueue.Clear();
            this.fiberQueue.Clear();
        }

        public void Update()
        {
            int count = this.fiberQueue.Count;
            while (count-- > 0)
            {
                if (!this.fiberQueue.TryDequeue(out Fiber fiber))
                {
                    continue;
                }
                if (fiber == null)
                {
                    continue;
                }
                
                if (fiber.IsDisposed)
                {
                    continue;
                }
                this.fiberQueue.Enqueue(fiber);

                fiber.Update();
            }
        }

        public void LateUpdate()
        {
            int count = this.fiberQueue.Count;
            while (count-- > 0)
            {
                if (!this.fiberQueue.TryDequeue(out Fiber fiber))
                {
                    continue;
                }
                if (fiber == null)
                {
                    continue;
                }

                if (fiber.IsDisposed)
                {
                    continue;
                }

                this.fiberQueue.Enqueue(fiber);
                
                fiber.LateUpdate();
            }

            while (this.addQueue.Count > 0)
            {
                this.addQueue.TryDequeue(out Fiber fiber);
                this.fiberQueue.Enqueue(fiber);
            }
        }


        public void AddToScheduler(Fiber fiber)
        {
            fiber.ThreadSynchronizationContext.ThreadId = this.threadId;
            this.addQueue.Enqueue(fiber);
        }
    }
}
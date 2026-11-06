using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace ET
{
    /// <summary>
    /// 线程池负载均衡纤程调度器（对应 <see cref="SchedulerType.ThreadPool"/>）。
    /// <para>
    /// 【工作原理】
    /// 启动与 CPU 逻辑核心数（<see cref="Environment.ProcessorCount"/>）相等的工作线程，共享一个并发纤程队列 <see cref="fiberQueue"/>。
    /// 工作线程每次从队列中 <c>TryDequeue</c> 取出一个 <see cref="Fiber"/>，将其 <c>ThreadSynchronizationContext.ThreadId</c> 临时绑定为当前工作线程 ID，
    /// 完整执行完一次 <c>Update()</c> 和 <c>LateUpdate()</c> 后，再将 <c>ThreadId</c> 置 0 并重新 <c>Enqueue</c> 放回队尾！
    /// 这样既保证了<b>同一个 Fiber 在任意时刻绝不会被两个线程同时执行（严格单线程语义）</b>，又实现了大量轻量级 Fiber 在多核 CPU 间的自动负载均衡！
    /// </para>
    /// </summary>
    internal class ThreadPoolScheduler: IScheduler
    {
        private volatile bool isDisposed;
        
        private readonly List<Thread> threads;

        private readonly ConcurrentQueue<Fiber> fiberQueue = new();

        public ThreadPoolScheduler()
        {
            int threadCount = Environment.ProcessorCount;
            this.threads = new List<Thread>(threadCount);
            for (int i = 0; i < threadCount; ++i)
            {
                Thread thread = new(this.Update);
                this.threads.Add(thread);
                thread.Start();
            }
        }

        private void Update()
        {
            int count = 0;
            int threadId = Environment.CurrentManagedThreadId;
            
            while (true)
            {
                if (count <= 0)
                {
                    Thread.Sleep(1);
                    
                    // count最小为1
                    count = this.fiberQueue.Count / this.threads.Count + 1;
                }

                --count;
                
                if (this.isDisposed)
                {
                    return;
                }
                
                if (!this.fiberQueue.TryDequeue(out Fiber fiber))
                {
                    Thread.Sleep(1);
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

                fiber.ThreadSynchronizationContext.ThreadId = threadId;
                fiber.Update();
                fiber.LateUpdate();
                fiber.ThreadSynchronizationContext.ThreadId = 0;
                
                this.fiberQueue.Enqueue(fiber);
            }
        }

        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }
            
            this.isDisposed = true;
            
            foreach (Thread thread in this.threads)
            {
                thread.Join();
            }
            this.threads.Clear();
        }

        public void AddToScheduler(Fiber fiber)
        {
            this.fiberQueue.Enqueue(fiber);
        }
    }
}
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace ET
{
    /// <summary>
    /// 线程同步上下文（继承自 .NET 的 <see cref="SynchronizationContext"/>）。
    /// <remarks>
    /// 核心作用：将跨线程的异步回调（例如底层 Socket 异步收发回调、跨线程投递的任务）通过无锁并发队列收集起来，
    /// 并由绑定了该上下文的 Fiber 所在线程在 <see cref="Update"/> 中统一串行执行，从而保证单 Fiber 内部逻辑绝对的单线程安全，无需加锁。
    /// </remarks>
    /// </summary>
    public class ThreadSynchronizationContext : SynchronizationContext
    {
        // 线程同步队列,发送接收socket回调都放到该队列,由poll线程统一执行
        private readonly ConcurrentQueue<Action> queue = new();

        /// <summary>
        /// 临时缓存出队的委托对象。
        /// </summary>
        private Action a;
        
        /// <summary>
        /// 当前同步上下文绑定的受管线程 ID。
        /// </summary>
        internal int ThreadId { get; set; }

        /// <summary>
        /// 在当前线程帧循环中轮询并执行所有已投递的跨线程回调任务。
        /// </summary>
        public void Update()
        {
            while (true)
            {
                if (!this.queue.TryDequeue(out a))
                {
                    return;
                }

                try
                {
                    a();
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        /// <summary>
        /// 重写 <see cref="SynchronizationContext.Post"/>，支持 C# 原生 <c>await</c> 切回当前同步上下文。
        /// </summary>
        public override void Post(SendOrPostCallback callback, object state)
        {
            this.Post(() => callback(state));
        }
		
        /// <summary>
        /// 向当前同步上下文队列投递一个无参委托，等待目标线程在下一帧 <see cref="Update"/> 中执行。
        /// </summary>
        public void Post(Action action)
        {
            this.queue.Enqueue(action);
        }
    }
}
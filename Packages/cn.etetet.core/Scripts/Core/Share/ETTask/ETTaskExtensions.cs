using System;
using System.Collections.Generic;

namespace ET
{
    public partial class ETTask
    {
        /// <summary>
        /// 异步获取当前协程调用链上绑定的强类型上下文对象（最常用的是 <c>await ETTask.GetContextAsync&lt;ETCancellationToken&gt;()</c>）。
        /// <para>
        /// 【工作原理】
        /// 创建一个标记为 <see cref="TaskType.ContextTask"/> 的特殊 <see cref="ETTask{T}"/> 并挂起；
        /// 外层状态机的 <c>AwaitUnsafeOnCompleted</c> 或 <c>SetContext</c> 会立刻把当前协程链上的 <c>Context</c> 通过 <c>SetResult</c> 传给它，从而在同一帧同步返回上下文且无需验证 EntityRef（标记了 <c>[SkipAwaitEntityCheck]</c>）。
        /// </para>
        /// </summary>
        [SkipAwaitEntityCheck]
        public static async ETTask<T> GetContextAsync<T>() where T: class
        {
            ETTask<object> tcs = ETTask<object>.Create(true);
            tcs.TaskType = TaskType.ContextTask;
            object ret = await tcs;
            return ret as T;
        }
        
        /// <summary>
        /// 异步获取当前协程调用链上绑定的通用 <see cref="object"/> 上下文。
        /// </summary>
        [SkipAwaitEntityCheck]
        public static async ETTask<object> GetContextAsync()
        {
            ETTask<object> tcs = ETTask<object>.Create(true);
            tcs.TaskType = TaskType.ContextTask;
            object ret = await tcs;
            return ret;
        }
        
        /// <summary>
        /// 内部并发等待计数器，用于实现 <see cref="WaitAll(ETTask[])"/> 和 <see cref="WaitAny(ETTask[])"/>。
        /// </summary>
        private class CoroutineBlocker
        {
            private int count;

            private ETTask tcs;

            public CoroutineBlocker(int count)
            {
                this.count = count;
            }
            
            public async ETTask RunSubCoroutineAsync(ETTask task)
            {
                try
                {
                    await task;
                }
                finally
                {
                    --this.count;
                
                    if (this.count <= 0 && this.tcs != null)
                    {
                        ETTask t = this.tcs;
                        this.tcs = null;
                        t.SetResult();
                    }
                }
            }

            public async ETTask WaitAsync()
            {
                if (this.count <= 0)
                {
                    return;
                }
                this.tcs = ETTask.Create(true);
                await tcs;
            }
        }

        /// <summary>
        /// 并发等待列表中的任意一个 <see cref="ETTask"/> 完成即返回（会自动将当前协程的 Context 传递给所有子任务）。
        /// </summary>
        public static async ETTask WaitAny(List<ETTask> tasks)
        {
            if (tasks.Count == 0)
            {
                return;
            }

            object context = await ETTask.GetContextAsync();
            
            CoroutineBlocker coroutineBlocker = new(1);

            foreach (ETTask task in tasks)
            {
                coroutineBlocker.RunSubCoroutineAsync(task).Coroutine(context);
            }

            await coroutineBlocker.WaitAsync();
        }

        /// <summary>
        /// 并发等待数组中的任意一个 <see cref="ETTask"/> 完成即返回。
        /// </summary>
        public static async ETTask WaitAny(ETTask[] tasks)
        {
            if (tasks.Length == 0)
            {
                return;
            }

            object context = await ETTask.GetContextAsync();
            CoroutineBlocker coroutineBlocker = new(1);

            foreach (ETTask task in tasks)
            {
                coroutineBlocker.RunSubCoroutineAsync(task).Coroutine(context);
            }

            await coroutineBlocker.WaitAsync();
        }

        /// <summary>
        /// 并发等待数组中的所有 <see cref="ETTask"/> 全部完成后返回（会自动将当前协程的 Context 透传给所有子任务）。
        /// </summary>
        public static async ETTask WaitAll(ETTask[] tasks)
        {
            if (tasks.Length == 0)
            {
                return;
            }

            object context = await ETTask.GetContextAsync();
            CoroutineBlocker coroutineBlocker = new(tasks.Length);

            foreach (ETTask task in tasks)
            {
                coroutineBlocker.RunSubCoroutineAsync(task).Coroutine(context);
            }

            await coroutineBlocker.WaitAsync();
        }

        /// <summary>
        /// 并发等待列表中的所有 <see cref="ETTask"/> 全部完成后返回（会自动将当前协程的 Context 透传给所有子任务）。
        /// </summary>
        public static async ETTask WaitAll(List<ETTask> tasks)
        {
            if (tasks.Count == 0)
            {
                return;
            }

            object context = await ETTask.GetContextAsync();
            CoroutineBlocker coroutineBlocker = new(tasks.Count);

            foreach (ETTask task in tasks)
            {
                coroutineBlocker.RunSubCoroutineAsync(task).Coroutine(context);
            }

            await coroutineBlocker.WaitAsync();
        }
    }
}
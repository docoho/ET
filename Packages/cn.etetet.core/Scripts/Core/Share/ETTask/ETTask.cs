using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace ET
{
    /// <summary>
    /// <see cref="IETTask"/> 协程上下文向下传递扩展工具。
    /// </summary>
    internal static class IETTaskExtension
    {
        /// <summary>
        /// 沿着异步调用链（从外层 awaiter 向内层被 await 的子任务）逐级传递协程上下文（如 <see cref="ETCancellationToken"/>）。
        /// 当遇到最底层的 <see cref="TaskType.ContextTask"/>（即调用了 <c>ETTask.GetContextAsync()</c> 的等待点）时，立即将该上下文作为结果唤醒它！
        /// </summary>
        internal static void SetContext(this IETTask task, object context)
        {
            while (true)
            {
                if (task.TaskType == TaskType.ContextTask)
                {
                    ((ETTask<object>)task).SetResult(context);
                    break;
                }

                // cancellationToken传下去
                task.TaskType = TaskType.WithContext;
                object child = task.Context;
                task.Context = context;
                task = child as IETTask;
                if (task == null)
                {
                    break;
                }
                //// 传递到WithContext为止，因为可能这一层设置了新的context
                if (task.TaskType == TaskType.WithContext)
                {
                    break;
                }
            }
        }
    }
    
    /// <summary>
    /// <see cref="ETTask"/> 的上下文任务类型枚举。
    /// </summary>
    public enum TaskType: byte
    {
        /// <summary>普通异步任务（此时 <see cref="IETTask.Context"/> 临时指向当前正在 await 的子任务，形成调用链单向链表）</summary>
        Common,
        /// <summary>已绑定显式上下文（如通过 <c>NewContext</c> 或 <c>Coroutine(context)</c> 注入了 <see cref="ETCancellationToken"/>）的任务</summary>
        WithContext,
        /// <summary>专门用于获取当前协程链上下文的特殊任务（由 <see cref="ETTask.GetContextAsync"/> 创建）</summary>
        ContextTask,
    }
    
    /// <summary>
    /// ETTask 统一接口，用于支持在异步调用链中免参数透传上下文对象（<see cref="Context"/>）。
    /// </summary>
    public interface IETTask
    {
        public TaskType TaskType { get; set; }
        public object Context { get; set; }
    }
    
    /// <summary>
    /// ET10 轻量级、零 GC、支持上下文透传的无返回值异步任务类（替代 C# 原生 <see cref="System.Threading.Tasks.Task"/>）。
    /// </summary>
    /// <remarks>
    /// 【初学者必读 —— 为什么 ET 不用 C# 原生的 <c>Task</c>，而要自己实现 <c>ETTask</c>？】
    /// 1. <b>极致性能与对象池复用（零 GC）</b>：
    ///    C# 原生 <c>Task</c> 包含大量多线程同步、安全上下文捕获（<c>ExecutionContext</c>）等重型字段，每次异步调用都会产生堆内存分配。
    ///    而 <see cref="ETTask"/> 专为游戏单线程纤程（<see cref="Fiber"/>）设计，自身既是 Task 又是 Awaiter，且配合 <see cref="ETAsyncTaskMethodBuilder"/> 和 <see cref="StateMachineWrap{T}"/> 实现了状态机与任务对象的全链路池化复用！
    /// 2. <b>隐式上下文传递（告别层层传递 <c>ETCancellationToken</c> 参数）</b>：
    ///    在传统写法中，如果想在深层异步函数里支持取消，必须把 <c>ETCancellationToken</c> 作为参数一层层传进 10 个函数签名里。
    ///    而在 ET10 中，外层只需调用 <c>await DoSomethingAsync().NewContext(cancellationToken)</c>，深层任意位置只需调用 <c>await ETTask.GetContextAsync&lt;ETCancellationToken&gt;()</c> 即可直接拿到该取消令牌！
    /// 3. <b>严禁使用 <c>async void</c></b>：
    ///    当你在同步方法里启动一个不需要等待的异步方法时，请让异步方法返回 <see cref="ETTask"/>，并在调用处加上 <c>.Coroutine()</c>（例如 <c> EnterMapAsync().Coroutine();</c>）。
    /// </remarks>
    [AsyncMethodBuilder(typeof (ETAsyncTaskMethodBuilder))]
    public partial class ETTask: ICriticalNotifyCompletion, IETTask
    {
        /// <summary>
        /// 全局未捕获异步异常处理器（通常在启动时绑定到 <c>Log.Error</c>）。
        /// </summary>
        [StaticField]
        public static Action<Exception> ExceptionHandler;

        [StaticField]
        private static ETTask completedTask;
        
        /// <summary>
        /// 获取一个已经处于完成状态（Succeeded）的全局单例 <see cref="ETTask"/>（不可回收，用于同步完成的快速返回分支）。
        /// </summary>
        [StaticField]
        public static ETTask CompletedTask
        {
            [SkipAwaitEntityCheck]
            get
            {
                return completedTask ??= new ETTask() { state = AwaiterStatus.Succeeded };
            }
        }

        /// <summary>
        /// <see cref="ETTask"/> 静态并发对象池队列（最大缓存 1000 个实例）。
        /// </summary>
        [StaticField]
        private static readonly ConcurrentQueue<ETTask> queue = new();

        /// <summary>
        /// 创建一个手动控制的 <see cref="ETTask"/>（类似于原生 C# 的 <c>TaskCompletionSource</c>）。
        /// <para>
        /// 请不要随便使用ETTask的对象池，除非你完全搞懂了ETTask!!!
        /// 假如开启了池,await之后不能再操作ETTask，否则可能操作到再次从池中分配出来的ETTask，产生灾难性的后果
        /// SetResult的时候请现将tcs置空，避免多次对同一个ETTask SetResult
        /// </para>
        /// </summary>
        [DebuggerHidden]
        public static ETTask Create(bool fromPool = false)
        {
            if (!fromPool)
            {
                return new ETTask();
            }
            if (!queue.TryDequeue(out ETTask task))
            {
                return new ETTask() {fromPool = true}; 
            }
            return task;
        }

        /// <summary>
        /// 当 <see cref="GetResult"/> 被调用（即 await 结束）时，若该任务来自对象池，则重置状态并放回池中。
        /// </summary>
        [DebuggerHidden]
        private void Recycle()
        {
            if (!this.fromPool)
            {
                return;
            }
            
            this.state = AwaiterStatus.Pending;
            this.callback = null;
            this.Context = null;
            this.TaskType = TaskType.Common;
            // 太多了
            if (queue.Count > 1000)
            {
                return;
            }
            queue.Enqueue(this);
        }

        private bool fromPool;
        private AwaiterStatus state;
        private object callback; // Action or ExceptionDispatchInfo

        [DebuggerHidden]
        private ETTask()
        {
            this.TaskType = TaskType.Common;
        }
        
        [DebuggerHidden]
        private async ETVoid InnerCoroutine()
        {
            await this;
        }

        /// <summary>
        /// 以“发后不管（Fire-and-Forget）”的方式独立启动当前异步协程（替代 <c>async void</c>），内部异常会自动转发给 <see cref="ExceptionHandler"/>。
        /// </summary>
        [DebuggerHidden]
        public void Coroutine()
        {
            this.SetContext(null);
            InnerCoroutine().Coroutine();
        }
        
        /// <summary>
        /// 携带指定的协程上下文（如 <see cref="ETCancellationToken"/>）独立启动当前异步协程。
        /// </summary>
        [DebuggerHidden]
        public void Coroutine(object context)
        {
            this.SetContext(context);
            InnerCoroutine().Coroutine();
        }
        
        /// <summary>
        /// 在 await 当前任务的同时，为该子协程链注入一个新的上下文对象（例如新的 <see cref="ETCancellationToken"/>）。
        /// </summary>
        [DebuggerHidden]
        public async ETTask NewContext(object context)
        {
            this.SetContext(context);
            await this;
        }

        /// <summary>
        /// 编译器 <c>await</c> 语法糖入口（返回自身，避免额外分配 Awaiter 对象）。
        /// </summary>
        [DebuggerHidden]
        public ETTask GetAwaiter()
        {
            return this;
        }

        /// <summary>
        /// 判断当前异步任务是否已完成（Succeeded 或 Faulted）。
        /// </summary>
        public bool IsCompleted
        {
            [DebuggerHidden]
            get
            {
                return this.state != AwaiterStatus.Pending;
            }
        }

        /// <summary>
        /// 注册异步完成后的状态机恢复回调（<c>MoveNext</c>），不捕获 ExecutionContext，性能最高。
        /// </summary>
        [DebuggerHidden]
        public void UnsafeOnCompleted(Action action)
        {
            if (this.state != AwaiterStatus.Pending)
            {
                action?.Invoke();
                return;
            }

            this.callback = action;
        }

        [DebuggerHidden]
        public void OnCompleted(Action action)
        {
            this.UnsafeOnCompleted(action);
        }

        /// <summary>
        /// 异步等待完成时由编译器状态机调用以获取结果（或重新抛出捕获的异常），并触发对象池回收。
        /// 严禁在未 <c>await</c> 完成前直接手动调用此方法！
        /// </summary>
        [DebuggerHidden]
        public void GetResult()
        {
            switch (this.state)
            {
                case AwaiterStatus.Succeeded:
                    this.Recycle();
                    break;
                case AwaiterStatus.Faulted:
                    ExceptionDispatchInfo c = this.callback as ExceptionDispatchInfo;
                    this.callback = null;
                    this.Recycle();
                    c?.Throw();
                    break;
                default:
                    throw new NotSupportedException("ETTask does not allow call GetResult directly when task not completed. Please use 'await'.");
            }
        }

        /// <summary>
        /// 将当前任务标记为成功完成，并立即同步触发挂起的 <c>await</c> 后续状态机回调（<see cref="callback"/>）。
        /// </summary>
        [DebuggerHidden]
        public void SetResult()
        {
            if (this.state != AwaiterStatus.Pending)
            {
                throw new InvalidOperationException("TaskT_TransitionToFinal_AlreadyCompleted");
            }

            this.state = AwaiterStatus.Succeeded;

            Action c = this.callback as Action;
            this.callback = null;
            c?.Invoke();
        }

        /// <summary>
        /// 将当前任务标记为异常失败，保留原始异常堆栈（<see cref="ExceptionDispatchInfo"/>）并唤醒等待者抛出异常。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerHidden]
        public void SetException(Exception e)
        {
            if (this.state != AwaiterStatus.Pending)
            {
                throw new InvalidOperationException("TaskT_TransitionToFinal_AlreadyCompleted");
            }

            this.state = AwaiterStatus.Faulted;

            Action c = this.callback as Action;
            this.callback = ExceptionDispatchInfo.Capture(e);
            c?.Invoke();
        }

        public TaskType TaskType { get; set; }
        public object Context { get; set; }
    }

    /// <summary>
    /// ET10 轻量级、零 GC、支持上下文透传的带返回值（<typeparamref name="T"/>）异步任务类（替代 C# 原生 <c>Task&lt;T&gt;</c>）。
    /// </summary>
    [AsyncMethodBuilder(typeof (ETAsyncTaskMethodBuilder<>))]
    public class ETTask<T>: ICriticalNotifyCompletion, IETTask
    {
        [StaticField]
        private static readonly ConcurrentQueue<ETTask<T>> queue = new();
        
        /// <summary>
        /// 创建一个手动控制的 <see cref="ETTask{T}"/>。
        /// <para>
        /// 请不要随便使用ETTask的对象池，除非你完全搞懂了ETTask!!!
        /// 假如开启了池,await之后不能再操作ETTask，否则可能操作到再次从池中分配出来的ETTask，产生灾难性的后果
        /// SetResult的时候请现将tcs置空，避免多次对同一个ETTask SetResult
        /// </para>
        /// </summary>
        [DebuggerHidden]
        public static ETTask<T> Create(bool fromPool = false)
        {
            if (!fromPool)
            {
                return new ETTask<T>();
            }
            
            if (!queue.TryDequeue(out ETTask<T> task))
            {
                return new ETTask<T>() {fromPool = true}; 
            }
            return task;
        }
        
        [DebuggerHidden]
        private void Recycle()
        {
            if (!this.fromPool)
            {
                return;
            }
            this.callback = null;
            this.value = default;
            this.state = AwaiterStatus.Pending;
            this.Context = null;
            this.TaskType = TaskType.Common;
            // 太多了
            if (queue.Count > 1000)
            {
                return;
            }
            queue.Enqueue(this);
        }

        private bool fromPool;
        private AwaiterStatus state;
        private T value;
        private object callback; // Action or ExceptionDispatchInfo

        [DebuggerHidden]
        private ETTask()
        {
            this.TaskType = TaskType.Common;
        }

        [DebuggerHidden]
        private async ETVoid InnerCoroutine()
        {
            await this;
        }

        /// <summary>
        /// 以“发后不管（Fire-and-Forget）”的方式独立启动当前异步协程（忽略返回值）。
        /// </summary>
        [DebuggerHidden]
        public void Coroutine()
        {
            this.SetContext(null);
            InnerCoroutine().Coroutine();
        }
        
        /// <summary>
        /// 携带指定的协程上下文（如 <see cref="ETCancellationToken"/>）独立启动当前异步协程。
        /// </summary>
        [DebuggerHidden]
        public void Coroutine(object context)
        {
            this.SetContext(context);
            InnerCoroutine().Coroutine();
        }
        
        /// <summary>
        /// 在 await 的同时可以换一个新的 cancellationToken 或上下文对象。
        /// </summary>
        [DebuggerHidden]
        public async ETTask<T> NewContext(object context)
        {
            this.SetContext(context);
            return await this;
        }

        [DebuggerHidden]
        public ETTask<T> GetAwaiter()
        {
            return this;
        }

        /// <summary>
        /// 获取异步任务的返回值 <typeparamref name="T"/>，并将池化任务实例回收到对象池中。
        /// </summary>
        [DebuggerHidden]
        public T GetResult()
        {
            switch (this.state)
            {
                case AwaiterStatus.Succeeded:
                    T v = this.value;
                    this.Recycle();
                    return v;
                case AwaiterStatus.Faulted:
                    ExceptionDispatchInfo c = this.callback as ExceptionDispatchInfo;
                    this.callback = null;
                    this.Recycle();
                    c?.Throw();
                    return default;
                default:
                    throw new NotSupportedException("ETask does not allow call GetResult directly when task not completed. Please use 'await'.");
            }
        }
        
        public bool IsCompleted
        {
            [DebuggerHidden]
            get
            {
                return state != AwaiterStatus.Pending;
            }
        } 

        [DebuggerHidden]
        public void UnsafeOnCompleted(Action action)
        {
            if (this.state != AwaiterStatus.Pending)
            {
                action?.Invoke();
                return;
            }

            this.callback = action;
        }

        [DebuggerHidden]
        public void OnCompleted(Action action)
        {
            this.UnsafeOnCompleted(action);
        }

        /// <summary>
        /// 设置异步任务的返回值 <paramref name="result"/>，将状态标记为完成并立即唤醒等待方。
        /// </summary>
        [DebuggerHidden]
        public void SetResult(T result)
        {
            if (this.state != AwaiterStatus.Pending)
            {
                throw new InvalidOperationException("TaskT_TransitionToFinal_AlreadyCompleted");
            }

            this.state = AwaiterStatus.Succeeded;

            this.value = result;

            Action c = this.callback as Action;
            this.callback = null;
            c?.Invoke();
        }
        
        /// <summary>
        /// 设置异步任务异常，将状态标记为 Faulted 并唤醒等待方重新抛出该异常。
        /// </summary>
        [DebuggerHidden]
        public void SetException(Exception e)
        {
            if (this.state != AwaiterStatus.Pending)
            {
                throw new InvalidOperationException("TaskT_TransitionToFinal_AlreadyCompleted");
            }

            this.state = AwaiterStatus.Faulted;

            Action c = this.callback as Action;
            this.callback = ExceptionDispatchInfo.Capture(e);
            c?.Invoke();
        }

        public TaskType TaskType { get; set; }
        public object Context { get; set; }
    }
}
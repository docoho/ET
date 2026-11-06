using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security;

namespace ET
{
    /// <summary>
    /// 自定义 C# 编译器 <c>async ETTask</c> 状态机构建器（MethodBuilder）。
    /// <para>
    /// 【核心作用】
    /// 1. 在异步方法启动时，自动从 <see cref="ETTask"/> 对象池取出任务实例；
    /// 2. 在遇到未同步完成的 <c>await</c> 挂起点时，通过 <see cref="StateMachineWrap{T}"/> 从对象池借用状态机包装器，避免每次挂起产生闭包 Delegate 分配；
    /// 3. 在 <c>AwaitUnsafeOnCompleted</c> 中自动维护父子 <see cref="IETTask"/> 的 <c>Context</c> 引用链，实现 <see cref="ETCancellationToken"/> 等上下文的零侵入透传。
    /// </para>
    /// </summary>
    public struct ETAsyncTaskMethodBuilder
    {
        private IStateMachineWrap iStateMachineWrap;
        
        private ETTask tcs;

        // 1. Static Create method.
        [DebuggerHidden]
        public static ETAsyncTaskMethodBuilder Create()
        {
            ETAsyncTaskMethodBuilder builder = new() { tcs = ETTask.Create(true) };
            return builder;
        }

        // 2. TaskLike Task property.
        [DebuggerHidden]
        public ETTask Task => this.tcs;

        // 3. SetException
        [DebuggerHidden]
        public void SetException(Exception exception)
        {
            if (this.iStateMachineWrap != null)
            {
                this.iStateMachineWrap.Recycle();
                this.iStateMachineWrap = null;
            }
            this.tcs.SetException(exception);
        }

        // 4. SetResult
        [DebuggerHidden]
        public void SetResult()
        {
            if (this.iStateMachineWrap != null)
            {
                this.iStateMachineWrap.Recycle();
                this.iStateMachineWrap = null;
            }
            this.tcs.SetResult();
        }

        // 5. AwaitOnCompleted
        [DebuggerHidden]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : class, IETTask, INotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            this.iStateMachineWrap ??= StateMachineWrap<TStateMachine>.Fetch(ref stateMachine);
            awaiter.OnCompleted(this.iStateMachineWrap.MoveNext);
        }

        // 6. AwaitUnsafeOnCompleted
        [DebuggerHidden]
        [SecuritySafeCritical]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            this.iStateMachineWrap ??= StateMachineWrap<TStateMachine>.Fetch(ref stateMachine);
            awaiter.UnsafeOnCompleted(this.iStateMachineWrap.MoveNext);

            if (awaiter is not IETTask task)
            {
                return;
            }

            // 如果当前外层任务已经带有 Context，则直接向内层被 await 的子任务传递 Context
            if (this.tcs.TaskType == TaskType.WithContext)
            {
                task.SetContext(this.tcs.Context);
                return;
            }
            
            // 否则先将内层子任务挂载到当前任务的 Context 字段上，构成单向链表，等待更外层调用 SetContext 时顺着链表向下传递
            this.tcs.Context = task;
        }

        // 7. Start
        [DebuggerHidden]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        // 8. SetStateMachine
        [DebuggerHidden]
        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
        }
    }

    /// <summary>
    /// 自定义 C# 编译器 <c>async ETTask&lt;T&gt;</c> 泛型状态机构建器。
    /// </summary>
    public struct ETAsyncTaskMethodBuilder<T>
    {
        private IStateMachineWrap iStateMachineWrap;
        
        private ETTask<T> tcs;

        // 1. Static Create method.
        [DebuggerHidden]
        public static ETAsyncTaskMethodBuilder<T> Create()
        {
            ETAsyncTaskMethodBuilder<T> builder = new ETAsyncTaskMethodBuilder<T>() { tcs = ETTask<T>.Create(true) };
            return builder;
        }

        // 2. TaskLike Task property.
        [DebuggerHidden]
        public ETTask<T> Task => this.tcs;

        // 3. SetException
        [DebuggerHidden]
        public void SetException(Exception exception)
        {
            if (this.iStateMachineWrap != null)
            {
                this.iStateMachineWrap.Recycle();
                this.iStateMachineWrap = null;
            }
            this.tcs.SetException(exception);
        }

        // 4. SetResult
        [DebuggerHidden]
        public void SetResult(T ret)
        {
            if (this.iStateMachineWrap != null)
            {
                this.iStateMachineWrap.Recycle();
                this.iStateMachineWrap = null;
            }
            this.tcs.SetResult(ret);
        }

        // 5. AwaitOnCompleted
        [DebuggerHidden]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            this.iStateMachineWrap ??= StateMachineWrap<TStateMachine>.Fetch(ref stateMachine);
            awaiter.OnCompleted(this.iStateMachineWrap.MoveNext);
        }

        // 6. AwaitUnsafeOnCompleted
        [DebuggerHidden]
        [SecuritySafeCritical]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            this.iStateMachineWrap ??= StateMachineWrap<TStateMachine>.Fetch(ref stateMachine);
            awaiter.UnsafeOnCompleted(this.iStateMachineWrap.MoveNext);
            
            if (awaiter is not IETTask task)
            {
                return;
            }
            
            if (this.tcs.TaskType == TaskType.WithContext)
            {
                task.SetContext(this.tcs.Context);
                return;
            }
            
            this.tcs.Context = task;
        }

        // 7. Start
        [DebuggerHidden]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        // 8. SetStateMachine
        [DebuggerHidden]
        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
        }
    }
}
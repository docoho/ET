using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security;

namespace ET
{
    /// <summary>
    /// 仅供内部 <see cref="ETVoid"/> 使用的编译器状态机构建器。
    /// 当顶层 <c>.Coroutine()</c> 链条中抛出未捕获异常时，在此处的 <see cref="SetException"/> 统一转交给 <see cref="ETTask.ExceptionHandler"/> 打印错误日志。
    /// </summary>
    internal struct AsyncETVoidMethodBuilder
    {
        private IStateMachineWrap iStateMachineWrap;
        
        // 1. Static Create method.
        [DebuggerHidden]
        public static AsyncETVoidMethodBuilder Create()
        {
            AsyncETVoidMethodBuilder builder = new();
            return builder;
        }

        // 2. TaskLike Task property(void)
        [DebuggerHidden]
        public ETVoid Task => default;

        // 3. SetException
        [DebuggerHidden]
        public void SetException(Exception e)
        {
            if (this.iStateMachineWrap != null)
            {
                this.iStateMachineWrap.Recycle();
                this.iStateMachineWrap = null;
            }
            ETTask.ExceptionHandler.Invoke(e);
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
        }

        // 5. AwaitOnCompleted
        [DebuggerHidden]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : IETTask, INotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            this.iStateMachineWrap ??= StateMachineWrap<TStateMachine>.Fetch(ref stateMachine);
            awaiter.OnCompleted(this.iStateMachineWrap.MoveNext);
        }

        // 6. AwaitUnsafeOnCompleted
        [DebuggerHidden]
        [SecuritySafeCritical]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : IETTask, ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            this.iStateMachineWrap ??= StateMachineWrap<TStateMachine>.Fetch(ref stateMachine);
            awaiter.UnsafeOnCompleted(this.iStateMachineWrap.MoveNext);
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
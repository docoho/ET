using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ET
{
    /// <summary>
    /// 内部专用的零分配无返回值异步驱动结构体（仅由 <see cref="ETTask.InnerCoroutine"/> 内部使用，业务层禁止直接使用，业务层请统一返回 <see cref="ETTask"/> 并调用 <c>.Coroutine()</c>）。
    /// </summary>
    [AsyncMethodBuilder(typeof (AsyncETVoidMethodBuilder))]
    internal struct ETVoid: ICriticalNotifyCompletion
    {
        [DebuggerHidden]
        public void Coroutine()
        {
        }

        [DebuggerHidden]
        public bool IsCompleted => true;

        [DebuggerHidden]
        public void OnCompleted(Action continuation)
        {
        }

        [DebuggerHidden]
        public void UnsafeOnCompleted(Action continuation)
        {
        }
    }
}
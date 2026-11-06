using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace ET
{
    /// <summary>
    /// 编译器异步状态机包装接口（用于池化管理 <see cref="IAsyncStateMachine"/> 的装箱与 <c>MoveNext</c> 委托）。
    /// </summary>
    public interface IStateMachineWrap
    {
        Action MoveNext { get; }
        void Recycle();
    }
    
    /// <summary>
    /// 泛型异步状态机池化包装器。
    /// <para>
    /// 【性能优化原理】
    /// C# 编译器为每个 <c>async</c> 方法生成一个实现了 <see cref="IAsyncStateMachine"/> 的值类型结构体。
    /// 当遇到需要异步挂起的 <c>await</c> 时，必须将状态机保存起来并生成一个 <see cref="Action"/> 回调委托传给 Awaiter。
    /// <see cref="StateMachineWrap{T}"/> 通过泛型静态对象池缓存包装实例及其 <see cref="MoveNext"/> 委托，彻底消除了每次 <c>await</c> 挂起时的装箱（Boxing）和 Delegate 内存分配！
    /// </para>
    /// </summary>
    public class StateMachineWrap<T>: IStateMachineWrap where T: IAsyncStateMachine
    {
        [StaticField]
        private static readonly ConcurrentQueue<StateMachineWrap<T>> queue = new();

        /// <summary>
        /// 从对象池取出一个状态机包装器，并将编译器状态机结构体拷贝进去。
        /// </summary>
        public static StateMachineWrap<T> Fetch(ref T stateMachine)
        {
            if (!queue.TryDequeue(out var stateMachineWrap))
            {
                stateMachineWrap = new StateMachineWrap<T>();
            }
            stateMachineWrap.StateMachine = stateMachine;
            return stateMachineWrap;
        }
        
        /// <summary>
        /// 当异步方法执行完毕（SetResult / SetException）时，清空状态机字段并将包装器归还到对象池。
        /// </summary>
        public void Recycle()
        {
            if (queue.Count > 100)
            {
                return;
            }

            this.StateMachine = default;
            queue.Enqueue(this);
        }

        private readonly Action moveNext;

        public Action MoveNext
        {
            get
            {
                return this.moveNext;
            }
        }

        private T StateMachine;

        private StateMachineWrap()
        {
            this.moveNext = this.Run;
        }

        private void Run()
        {
            this.StateMachine.MoveNext();
        }
    }
}
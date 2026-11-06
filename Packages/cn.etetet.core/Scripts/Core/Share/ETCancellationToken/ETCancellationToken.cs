using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ET
{
    /// <summary>
    /// ET 专用轻量级协程取消令牌（替代 C# 原生 <c>CancellationTokenSource</c>）。
    /// </summary>
    /// <remarks>
    /// 【初学者指南 —— <c>ETCancellationToken</c> 是怎么工作的？】
    /// 1. <b>注册取消回调（<see cref="Add"/>）</b>：
    ///    底层挂起操作（如 <c>TimerComponent.WaitAsync</c> 或 <c>MoveComponent</c>）通过 <c>await ETTask.GetContextAsync&lt;ETCancellationToken&gt;()</c> 拿到当前协程绑定的令牌，
    ///    并将“移除定时器并唤醒 ETTask”的回调通过 <see cref="Add"/> 注册进来；如果正常等待结束，则通过 <see cref="Remove"/> 注销该回调。
    /// 2. <b>触发取消（<see cref="Cancel"/>）</b>：
    ///    当外部调用 <c>cancellationToken.Cancel()</c> 时，会立刻置空内部回调集合（此时 <see cref="IsDispose"/> / <c>IsCancel()</c> 变为 <c>true</c>），
    ///    并遍历执行所有已注册的取消回调，让挂起的 <c>await</c> 立即提前返回！
    /// 3. <b>注意</b>：在 <c>await</c> 返回后，业务层通常需要检查 <c>if (cancellationToken.IsCancel()) return;</c> 来判断是因为正常完成返回还是被取消中断返回。
    /// </remarks>
    public class ETCancellationToken
    {
        /// <summary>存储所有已注册的取消回调（使用对象池化的 <see cref="HashSetComponent{T}"/>，当被 Cancel 后该字段会被置为 null）</summary>
        private HashSetComponent<Action> actions = HashSetComponent<Action>.Create();

        /// <summary>可选附带的自定义上下文数据</summary>
        // 可以带一个数据
        public object Context;

        /// <summary>
        /// 注册一个取消时执行的回调委托。
        /// </summary>
        public void Add(Action callback)
        {
            // 如果action是null，绝对不能添加,要抛异常，说明有协程泄漏
            this.actions.Add(callback);
        }
        
        /// <summary>
        /// 注销一个已注册的取消回调（通常在异步等待正常完成时调用）。
        /// </summary>
        public void Remove(Action callback)
        {
            this.actions?.Remove(callback);
        }

        /// <summary>
        /// 判断当前取消令牌是否已经被触发取消（<c>actions == null</c>）。
        /// </summary>
        public bool IsDispose()
        {
            return this.actions == null;
        }

        /// <summary>
        /// 触发取消：立即执行所有已注册的取消回调，并将当前令牌标记为已取消（不可重复触发）。
        /// </summary>
        public void Cancel()
        {
            if (this.actions == null)
            {
                return;
            }

            this.Invoke();
        }

        private void Invoke()
        {
            using HashSetComponent<Action> runActions = this.actions;
            
            this.actions = null;
            try
            {
                foreach (Action action in runActions)
                {
                    action.Invoke();
                }
            }
            catch (Exception e)
            {
                ETTask.ExceptionHandler.Invoke(e);
            }
        }
    }
}
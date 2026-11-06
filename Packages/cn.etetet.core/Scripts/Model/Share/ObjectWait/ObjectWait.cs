using System;
using System.Collections.Generic;
using System.Linq;

namespace ET
{
    /// <summary>
    /// <see cref="ObjectWait"/> 异步等待结果的内置状态码常量。
    /// </summary>
    public static class WaitTypeError
    {
        /// <summary>
        /// 正常收到 <c>Notify</c> 唤醒。
        /// </summary>
        public const int Success = 0;
        /// <summary>
        /// 挂载 <see cref="ObjectWait"/> 的实体在等待期间被销毁。
        /// </summary>
        public const int Destroy = 1;
        /// <summary>
        /// 等待被协程取消令牌（<see cref="ETCancellationToken"/>）主动取消。
        /// </summary>
        public const int Cancel = 2;
        /// <summary>
        /// 等待超时。
        /// </summary>
        public const int Timeout = 3;
    }
    
    /// <summary>
    /// 所有可被 <see cref="ObjectWait"/> 等待的信号结构体必须实现的接口。
    /// </summary>
    public interface IWaitType
    {
        /// <summary>
        /// 等待结束时的状态码（参见 <see cref="WaitTypeError"/>）。
        /// </summary>
        int Error
        {
            get;
            set;
        }
    }
    
    /// <summary>
    /// 支持在 <see cref="ObjectWait"/> 组件销毁时统一触发结束回调的内部接口。
    /// </summary>
    public interface IDestroyRun
    {
        /// <summary>
        /// 以 <see cref="WaitTypeError.Destroy"/> 状态唤醒等待协程。
        /// </summary>
        void SetResult();
    }
    
    /// <summary>
    /// 针对特定信号类型 <typeparamref name="K"/> 的单次异步等待回调包装器。
    /// </summary>
    public class ResultCallback<K>: Object, IDestroyRun where K : struct, IWaitType
    {
        private ETTask<K> tcs;

        public ResultCallback()
        {
            this.tcs = ETTask<K>.Create(true);
        }

        public bool IsDisposed
        {
            get
            {
                return this.tcs == null;
            }
        }

        public ETTask<K> Task => this.tcs;

        /// <summary>
        /// 正常通知并传递信号数据 <paramref name="k"/>，唤醒等待的协程。
        /// </summary>
        public void SetResult(K k)
        {
            var t = tcs;
            this.tcs = null;
            t.SetResult(k);
        }

        /// <summary>
        /// 当 <see cref="ObjectWait"/> 销毁时调用，向等待协程返回 <see cref="WaitTypeError.Destroy"/> 结果。
        /// </summary>
        public void SetResult()
        {
            var t = tcs;
            this.tcs = null;
            t.SetResult(new K() { Error = WaitTypeError.Destroy });
        }
    }

    /// <summary>
    /// 实体级异步信号等待组件（挂载在 <see cref="Entity"/> 或 <see cref="Scene"/> 上）。
    /// <remarks>
    /// 核心用途：让一段协程逻辑可以挂起等待某个外部事件结构体（<c>Wait&lt;T&gt;()</c>），直到其他模块或网络消息调用 <c>Notify(new T())</c> 将其唤醒。
    /// 例如：移动协程等待移动停止信号、登录流程等待场景切换完成信号等，将原本割裂的回调状态机转化为线性直观的 <c>await</c> 代码。
    /// </remarks>
    /// </summary>
    [ComponentOf]
    public class ObjectWait: Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 当前正在挂起等待的信号类型到对应 <see cref="ResultCallback{K}"/> 的映射表。
        /// </summary>
        public Dictionary<Type, object> tcss = new Dictionary<Type, object>();
    }
}
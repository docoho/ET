using System;

namespace ET
{
    /// <summary>
    /// <see cref="EventSystem.Invoke{A}(long,A)"/> 回调处理器基础接口。
    /// </summary>
    public interface IInvoke
    {
        /// <summary>所处理的回调参数结构体类型</summary>
        Type Type { get; }
    }
    
    /// <summary>
    /// 无返回值的 Invoke 回调处理器抽象基类（需配合 <see cref="InvokeAttribute"/> 标记）。
    /// 例如定时器回调 <see cref="ATimer{T}"/> 即继承自此类。
    /// </summary>
    /// <typeparam name="A">回调参数结构体类型</typeparam>
    public abstract class AInvokeHandler<A>: HandlerObject, IInvoke where A: struct
    {
        public Type Type
        {
            get
            {
                return typeof (A);
            }
        }

        public abstract void Handle(A args);
    }
    
    /// <summary>
    /// 带返回值 <typeparamref name="T"/> 的 Invoke 回调处理器抽象基类（需配合 <see cref="InvokeAttribute"/> 标记）。
    /// 例如纤程初始化 <c>AInvokeHandler&lt;FiberInit, ETTask&gt;</c> 即继承自此类。
    /// </summary>
    /// <typeparam name="A">回调参数结构体类型</typeparam>
    /// <typeparam name="T">返回值类型（若为异步回调，则 <typeparamref name="T"/> 可以是 <see cref="ETTask"/>）</typeparam>
    public abstract class AInvokeHandler<A, T>: HandlerObject, IInvoke where A: struct
    {
        public Type Type
        {
            get
            {
                return typeof (A);
            }
        }

        public abstract T Handle(A args);
    }
}
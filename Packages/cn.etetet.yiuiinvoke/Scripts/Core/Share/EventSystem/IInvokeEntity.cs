using System;

namespace ET
{
    /// <summary>
    /// 携带目标Entity上下文的无返回值结构体Invoke处理器抽象基类。
    /// </summary>
    public abstract class AInvokeEntityHandler<A> : HandlerObject, IInvoke where A : struct
    {
        public Type Type
        {
            get
            {
                return typeof(A);
            }
        }

        public abstract void Handle(Entity entity, A args);
    }

    /// <summary>
    /// 携带目标Entity上下文且带返回值 <typeparamref name="T"/> 的结构体Invoke处理器抽象基类。
    /// </summary>
    public abstract class AInvokeEntityHandler<A, T> : HandlerObject, IInvoke where A : struct
    {
        public Type Type
        {
            get
            {
                return typeof(A);
            }
        }

        public abstract T Handle(Entity entity, A args);
    }
}
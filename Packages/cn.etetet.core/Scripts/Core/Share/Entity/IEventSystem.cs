using System;

namespace ET
{
    /// <summary>
    /// 实体周期事件（如 <see cref="UpdateEvent"/>、<see cref="LateUpdateEvent"/>）的泛型标记接口。
    /// 实现此接口的实体在挂载到场景树（<c>IsRegister = true</c>）时，会被自动注册到所在 <see cref="Fiber.EntitySystem"/> 对应的事件轮询队列中。
    /// </summary>
    public interface IEvent<T> where T: struct
    {
    }

    /// <summary>
    /// 标识一个 System 属于需要在 <see cref="Fiber.EntitySystem"/> 中维护轮询队列的事件型 System。
    /// </summary>
    public interface IEventSystem
    {
    }
    
    /// <summary>
    /// 针对特定结构体事件 <typeparamref name="T"/> 的实体系统调度接口。
    /// </summary>
    public interface AEventSystem<in T>: ISystemType, IEventSystem where T: struct
    {
        void Run(Entity e, T t);
    }
    
    /// <summary>
    /// 实体周期事件系统抽象基类（如 <see cref="UpdateSystem{T}"/> 和 <see cref="LateUpdateSystem{T}"/> 均继承自此类）。
    /// </summary>
    public abstract class EventSystem<E, T> : SystemObject, AEventSystem<T> where E: Entity, IEvent<T> where T: struct
    {
        public void Run(Entity e, T t)
        {
            this.Event((E)e, t);
        }

        public Type SystemType()
        {
            return typeof(AEventSystem<T>);
        }

        public Type Type()
        {
            return typeof(E);
        }

        protected abstract void Event(E e, T t);
    }
}
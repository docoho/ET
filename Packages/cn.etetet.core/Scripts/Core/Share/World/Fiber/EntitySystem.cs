using System;
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 纤程级 ECS 周期事件（如 <see cref="UpdateEvent"/>、<see cref="LateUpdateEvent"/>）轮询调度器（每个 <see cref="Fiber"/> 持有一个独立实例）。
    /// </summary>
    /// <remarks>
    /// 【巧妙的无删改出队入队轮询机制】
    /// 1. 当一个实现了 <see cref="IUpdate"/> 或 <see cref="ILateUpdate"/> 的实体挂载到场景树时，<see cref="RegisterSystem"/> 会将其 <see cref="EntityRef{Entity}"/> 压入对应的队列 <see cref="queues"/> 中；
    /// 2. 在每帧调用 <see cref="Publish{T}"/>（例如发布 <c>UpdateEvent</c>）时，先记录当前队列长度 <c>count</c>，每次从队首 <c>Dequeue()</c> 取出一个 <c>EntityRef&lt;Entity&gt;</c>：
    ///    - 如果实体已经在中途被销毁（<c>component == null || component.IsDisposed</c>），直接丢弃，不再放回队尾（实现 O(1) 零成本惰性移除！）；
    ///    - 如果实体仍然存活，则将其重新 <c>Enqueue()</c> 放回队尾供下一帧继续执行，并调用其对应的 <c>UpdateSystem.Run</c>！
    /// </remarks>
    public class EntitySystem
    {
        /// <summary>按事件系统类型（如 <c>typeof(AEventSystem&lt;UpdateEvent&gt;)</c>）分类存储的实体安全引用循环队列</summary>
        private readonly Dictionary<Type, Queue<EntityRef<Entity>>> queues = new();
        
        public Queue<EntityRef<Entity>> GetQueue(Type type)
        {
            if (!this.queues.TryGetValue(type, out var queue))
            {
                queue = new Queue<EntityRef<Entity>>();
                this.queues.Add(type, queue);
            }

            return queue;
        }
        
        /// <summary>
        /// 当实体挂载到场景树（<c>IsRegister = true</c>）时调用：如果该实体实现了任何周期事件系统（如 <c>IUpdate</c> / <c>ILateUpdate</c>），将其加入对应轮询队列。
        /// </summary>
        public virtual void RegisterSystem(Entity component)
        {
            Type type = component.GetType();

            TypeSystems.OneTypeSystems oneTypeSystems = EntitySystemSingleton.Instance.TypeSystems.GetOneTypeSystems(type);
            if (oneTypeSystems == null)
            {
                return;
            }

            foreach (Type queueType in oneTypeSystems.ClassType)
            {
                var queue = this.GetQueue(queueType);
                queue.Enqueue(component);
            }
        }
        
        /// <summary>
        /// 向当前纤程内所有注册了 <see cref="IEvent{T}"/> 的存活实体派发周期事件（如 <see cref="UpdateEvent"/> 或 <see cref="LateUpdateEvent"/>）。
        /// </summary>
        public void Publish<T>(T t) where T: struct
        {
            Type systemType = typeof(AEventSystem<T>);
            Queue<EntityRef<Entity>> queue = this.GetQueue(systemType);
            int count = queue.Count;
            while (count-- > 0)
            {
                Entity component = queue.Dequeue();
                if (component == null)
                {
                    continue;
                }
                if (component.IsDisposed)
                {
                    continue;
                }
                if (component is not IEvent<T>)
                {
                    continue;
                }
                try
                {
                    List<SystemObject> systems = EntitySystemSingleton.Instance.TypeSystems.GetSystems(component.GetType(), systemType);
                    if (systems == null)
                    {
                        continue;
                    }

                    queue.Enqueue(component);

                    foreach (AEventSystem<T> classSystem in systems)
                    {
                        try
                        {
                            classSystem.Run(component, t);
                        }
                        catch (Exception e)
                        {
                            Log.Error(e);
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new Exception($"entity system update fail: {component.GetType().FullName}", e);
                }

            }
        }
    }
}
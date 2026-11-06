using System;
using System.ComponentModel;

namespace ET
{
    /// <summary>
    /// 支持资源释放（<see cref="IDisposable"/>）、对象池回收以及序列化初始化钩子（<see cref="ISupportInitialize"/>）的抽象基类。
    /// </summary>
    /// <remarks>
    /// 【初学者指南】
    /// - <see cref="Entity"/> 和 <see cref="MessageObject"/> 都继承自本类。
    /// - 默认的 <see cref="Dispose"/> 实现会将对象回收到全局对象池 <see cref="ObjectPool"/> 中（如果对象标记了来自对象池）。
    /// - <see cref="BeginInit"/> 和 <see cref="EndInit"/> 是反序列化（如 MongoBson / MemoryPack）前后的生命周期回调钩子。
    /// </remarks>
    public abstract class DisposeObject: Object, IDisposable, ISupportInitialize
    {
        /// <summary>
        /// 释放当前对象。默认行为是将其回收到 <see cref="ObjectPool"/> 对象池中。
        /// </summary>
        public virtual void Dispose()
        {
            ObjectPool.Recycle(this);
        }
        
        /// <summary>
        /// 反序列化或初始化开始前触发的虚方法钩子。
        /// </summary>
        public virtual void BeginInit()
        {
        }
        
        /// <summary>
        /// 反序列化或初始化完成后触发的虚方法钩子（例如 Entity 树重建父子关系）。
        /// </summary>
        public virtual void EndInit()
        {
        }
    }

    /// <summary>
    /// 对象池可复用对象接口。实现此接口的类在通过 <see cref="ObjectPool"/> 存取时会自动管理池化标记与状态清理。
    /// </summary>
    public interface IPool
    {
        /// <summary>
        /// 标识当前实例是否是从对象池中取出的（决定回收时是否放回池中）。
        /// </summary>
        bool IsFromPool
        {
            get;
            set;
        }

        /// <summary>
        /// 当对象被回收到对象池时调用，用于重置内部字段状态，防止脏数据污染下一次复用。
        /// </summary>
        void Clear();
    }
}

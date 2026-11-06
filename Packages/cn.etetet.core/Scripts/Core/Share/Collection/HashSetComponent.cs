using System;
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 支持对象池复用的 <see cref="HashSet{T}"/> 包装类（禁止直接 <c>new</c>，需通过 <see cref="Create"/> 获取并配合 <c>using</c> 语句使用）。
    /// <remarks>
    /// 典型用法：<c>using HashSetComponent&lt;long&gt; set = HashSetComponent&lt;long&gt;.Create();</c>，离开作用域时自动清空并回收到 <see cref="ObjectPool"/>。
    /// </remarks>
    /// </summary>
    [DisableNew]
    public class HashSetComponent<T>: HashSet<T>, IPool, IDisposable
    {
        /// <summary>
        /// 从对象池中获取一个空的 <see cref="HashSetComponent{T}"/> 实例。
        /// </summary>
        public static HashSetComponent<T> Create()
        {
            return ObjectPool.Fetch(typeof (HashSetComponent<T>)) as HashSetComponent<T>;
        }

        /// <summary>
        /// 回收进对象池时自动清空集合内元素。
        /// </summary>
        void IPool.Clear()
        {
            base.Clear();
        }

        /// <summary>
        /// 释放并将当前集合实例归还到对象池。
        /// </summary>
        public void Dispose()
        {
            ObjectPool.Recycle(this);
        }

        /// <summary>
        /// 标记当前实例是否来自对象池。
        /// </summary>
        public bool IsFromPool { get; set; }
    }
}

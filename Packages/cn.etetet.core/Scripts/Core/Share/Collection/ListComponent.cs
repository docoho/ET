using System;
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 支持对象池复用的 <see cref="List{T}"/> 包装类（禁止直接 <c>new</c>，需通过 <see cref="Create"/> 获取并配合 <c>using</c> 语句使用）。
    /// <remarks>
    /// 典型用法：<c>using ListComponent&lt;Entity&gt; list = ListComponent&lt;Entity&gt;.Create();</c>，
    /// 离开作用域时若容量不超过 64 则自动清空并回收到 <see cref="ObjectPool"/>；若底层数组扩容超过 64，则直接丢弃交由 GC 回收，防止大数组常驻对象池造成内存膨胀。
    /// </remarks>
    /// </summary>
    [DisableNew]
    public class ListComponent<T>: List<T>, IPool, IDisposable
    {
        /// <summary>
        /// 从对象池中获取一个空的 <see cref="ListComponent{T}"/> 实例。
        /// </summary>
        public static ListComponent<T> Create()
        {
            return ObjectPool.Fetch(typeof (ListComponent<T>)) as ListComponent<T>;
        }

        /// <summary>
        /// 回收进对象池时自动清空列表元素。
        /// </summary>
        void IPool.Clear()
        {
            base.Clear();
        }

        /// <summary>
        /// 释放并将当前列表实例归还到对象池（容量超过 64 时不入池，交由 GC 回收）。
        /// </summary>
        public void Dispose()
        {
            if (this.Capacity > 64) // 超过64，让gc回收
            {
                return;
            }
            ObjectPool.Recycle(this);
        }

        /// <summary>
        /// 标记当前实例是否来自对象池。
        /// </summary>
        public bool IsFromPool { get; set; }
    }
}

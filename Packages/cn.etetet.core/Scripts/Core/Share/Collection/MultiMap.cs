using System;
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 按键排序的一对多映射字典（继承自定制红黑树实现的 <see cref="SortedDictionary{TKey, TValue}"/>，一个 Key 对应一个 <see cref="List{K}"/>）。
    /// <remarks>
    /// 内部自带 <c>List&lt;K&gt;</c> 缓存池（通过构造参数 <paramref name="maxPoolCount"/> 控制池上限），
    /// 在高频增删 Key（如 <see cref="TimerComponent"/> 中按时间戳添加/移除定时器列表）时避免频繁分配 <see cref="List{K}"/> 对象。
    /// </remarks>
    /// </summary>
    public class MultiMap<T, K>: SortedDictionary<T, List<K>>
    {
        private readonly List<K> Empty = new();
        private readonly int maxPoolCount;
        private readonly Queue<List<K>> pool;

        /// <summary>
        /// 初始化有序一对多字典，并指定内部 <see cref="List{K}"/> 缓存池的最大容量。
        /// </summary>
        public MultiMap(int maxPoolCount = 0)
        {
            this.maxPoolCount = maxPoolCount;
            this.pool = new Queue<List<K>>(maxPoolCount);
        }

        /// <summary>
        /// 从内部池中获取或新建一个 <see cref="List{K}"/>。
        /// </summary>
        private List<K> FetchList()
        {
            if (this.pool.Count > 0)
            {
                return this.pool.Dequeue();
            }
            return new List<K>(10);
        }

        /// <summary>
        /// 将清空后的 <see cref="List{K}"/> 归还到内部池中。
        /// </summary>
        private void Recycle(List<K> list)
        {
            if (list == null)
            {
                return;
            }
            if (this.pool.Count == this.maxPoolCount)
            {
                return;
            }
            list.Clear();
            this.pool.Enqueue(list);
        }

        /// <summary>
        /// 向指定键 <paramref name="t"/> 下的列表追加一个值 <paramref name="k"/>。
        /// </summary>
        public void Add(T t, K k)
        {
            List<K> list;
            this.TryGetValue(t, out list);
            if (list == null)
            {
                list = this.FetchList();
                this.Add(t, list);
            }
            list.Add(k);
        }

        /// <summary>
        /// 从指定键 <paramref name="t"/> 下的列表中移除单个值 <paramref name="k"/>（若列表变空则同时移除键并回收列表）。
        /// </summary>
        public bool Remove(T t, K k)
        {
            List<K> list;
            this.TryGetValue(t, out list);
            if (list == null)
            {
                return false;
            }
            if (!list.Remove(k))
            {
                return false;
            }
            if (list.Count == 0)
            {
                this.Remove(t);
            }
            return true;
        }

        /// <summary>
        /// 移除指定键 <paramref name="t"/> 及其对应的整个列表，并将该列表回收到内部池。
        /// </summary>
        public new bool Remove(T t)
        {
            List<K> list;
            this.TryGetValue(t, out list);
            if (list == null)
            {
                return false;
            }
            this.Recycle(list);
            return base.Remove(t);
        }

        /// <summary>
        /// 不返回内部的list,copy一份出来
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public K[] GetAll(T t)
        {
            List<K> list;
            this.TryGetValue(t, out list);
            if (list == null)
            {
                return Array.Empty<K>();
            }
            return list.ToArray();
        }

        /// <summary>
        /// 返回内部的list
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public new List<K> this[T t]
        {
            get
            {
                this.TryGetValue(t, out List<K> list);
                return list ?? Empty;
            }
        }

        /// <summary>
        /// 获取指定键下列表中的第一个元素（若不存在则返回默认值）。
        /// </summary>
        public K GetOne(T t)
        {
            List<K> list;
            this.TryGetValue(t, out list);
            if (list != null && list.Count > 0)
            {
                return list[0];
            }
            return default;
        }

        /// <summary>
        /// 判断指定键下的列表中是否包含指定的值。
        /// </summary>
        public bool Contains(T t, K k)
        {
            List<K> list;
            this.TryGetValue(t, out list);
            if (list == null)
            {
                return false;
            }
            return list.Contains(k);
        }
    }
}
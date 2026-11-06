using System;
using System.Collections.Generic;
using System.Linq;

namespace ET
{
    /// <summary>
    /// 基于哈希表的一对多去重集合字典（<c>Dictionary&lt;T, HashSet&lt;K&gt;&gt;</c>，一个 Key 对应一个不重复的 <see cref="HashSet{K}"/>）。
    /// <remarks>
    /// 访问不存在的 Key 时索引器返回共享的空集合 <see cref="Empty"/>（请勿向索引器返回的默认空集合直接 Add，应使用 <see cref="Add(T, K)"/> 方法）。
    /// </remarks>
    /// </summary>
    public class MultiMapSet<T, K>: Dictionary<T, HashSet<K>>
    {
        private readonly HashSet<K> Empty = new HashSet<K>();
		
        /// <summary>
        /// 向指定键 <paramref name="t"/> 下的集合中添加一个元素 <paramref name="k"/>（自动去重）。
        /// </summary>
        public void Add(T t, K k)
        {
            HashSet<K> list;
            this.TryGetValue(t, out list);
            if (list == null)
            {
                list = new HashSet<K>();
                this.Add(t, list);
            }
            list.Add(k);
        }

        /// <summary>
        /// 从指定键 <paramref name="t"/> 下的集合中移除元素 <paramref name="k"/>（若集合变空则同时移除该键）。
        /// </summary>
        public bool Remove(T t, K k)
        {
            HashSet<K> list;
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
        /// 不返回内部的list,copy一份出来
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public K[] GetAll(T t)
        {
            HashSet<K> list;
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
        public new HashSet<K> this[T t]
        {
            get
            {
                this.TryGetValue(t, out var list);
                return list ?? Empty;
            }
        }

        /// <summary>
        /// 获取指定键下集合中的任意一个元素（不存在则返回默认值）。
        /// </summary>
        public K GetOne(T t)
        {
            HashSet<K> list;
            this.TryGetValue(t, out list);
            if (list != null && list.Count > 0)
            {
                return list.FirstOrDefault();
            }
            return default;
        }

        /// <summary>
        /// 判断指定键下的集合是否包含指定元素。
        /// </summary>
        public bool Contains(T t, K k)
        {
            HashSet<K> list;
            this.TryGetValue(t, out list);
            if (list == null)
            {
                return false;
            }
            return list.Contains(k);
        }
    }
}
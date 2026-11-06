using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 无序的一对多去重集合字典（继承自 <see cref="Dictionary{TKey, TValue}"/>，一个 Key 对应一个 <see cref="HashSet{K}"/>）。
    /// <remarks>
    /// 注意其 <see cref="Count"/> 属性重写为了所有子 <see cref="HashSet{K}"/> 的元素总数之和。
    /// </remarks>
    /// </summary>
    public class UnOrderMultiMapSet<T, K>: Dictionary<T, HashSet<K>>
    {
        // 重用HashSet
        /// <summary>
        /// 获取指定键对应的 <see cref="HashSet{K}"/>（若不存在则返回一个新建的空集合）。
        /// </summary>
        public new HashSet<K> this[T t]
        {
            get
            {
                HashSet<K> set;
                if (!this.TryGetValue(t, out set))
                {
                    set = new HashSet<K>();
                }
                return set;
            }
        }
        
        /// <summary>
        /// 获取自身作为底层字典的引用。
        /// </summary>
        public Dictionary<T, HashSet<K>> GetDictionary()
        {
            return this;
        }
        
        /// <summary>
        /// 向指定键 <paramref name="t"/> 下的集合添加元素 <paramref name="k"/>。
        /// </summary>
        public void Add(T t, K k)
        {
            HashSet<K> set;
            this.TryGetValue(t, out set);
            if (set == null)
            {
                set = new HashSet<K>();
                base[t] = set;
            }
            set.Add(k);
        }

        /// <summary>
        /// 从指定键 <paramref name="t"/> 下的集合中移除元素 <paramref name="k"/>（若集合变空则同时移除该键）。
        /// </summary>
        public bool Remove(T t, K k)
        {
            HashSet<K> set;
            this.TryGetValue(t, out set);
            if (set == null)
            {
                return false;
            }
            if (!set.Remove(k))
            {
                return false;
            }
            if (set.Count == 0)
            {
                this.Remove(t);
            }
            return true;
        }

        /// <summary>
        /// 判断指定键下的集合是否包含指定元素。
        /// </summary>
        public bool Contains(T t, K k)
        {
            HashSet<K> set;
            this.TryGetValue(t, out set);
            if (set == null)
            {
                return false;
            }
            return set.Contains(k);
        }

        /// <summary>
        /// 获取所有键下所有 <see cref="HashSet{K}"/> 中元素的总个数。
        /// </summary>
        public new int Count
        {
            get
            {
                int count = 0;
                foreach (KeyValuePair<T,HashSet<K>> kv in this)
                {
                    count += kv.Value.Count;
                }
                return count;
            }
        }
    }
}
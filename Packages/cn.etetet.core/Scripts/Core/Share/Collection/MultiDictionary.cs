using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 双层嵌套字典（<c>Dictionary&lt;T, Dictionary&lt;M, N&gt;&gt;</c>），封装了二级键值（一级键 <typeparamref name="T"/> + 二级键 <typeparamref name="M"/> -> 值 <typeparamref name="N"/>）的增删改查。
    /// <remarks>
    /// 在添加元素时自动按需创建内层字典，在移除最后一个子键时自动清理外层键，避免残留空字典。
    /// </remarks>
    /// </summary>
    public class MultiDictionary<T, M, N>: Dictionary<T, Dictionary<M, N>>
    {
        /// <summary>
        /// 尝试根据一级键获取对应的内层字典。
        /// </summary>
        public bool TryGetDic(T t, out Dictionary<M, N> k)
        {
            return this.TryGetValue(t, out k);
        }

        /// <summary>
        /// 尝试根据一级键 <paramref name="t"/> 和二级键 <paramref name="m"/> 获取对应的值 <paramref name="n"/>。
        /// </summary>
        public bool TryGetValue(T t, M m, out N n)
        {
            n = default;

            if (!this.TryGetValue(t, out Dictionary<M, N> dic))
            {
                return false;
            }

            return dic.TryGetValue(m, out n);
        }

        /// <summary>
        /// 添加二级键值对（若一级键尚不存在则自动创建内层字典）。
        /// </summary>
        public void Add(T t, M m, N n)
        {
            Dictionary<M, N> kSet;
            this.TryGetValue(t, out kSet);
            if (kSet == null)
            {
                kSet = new Dictionary<M, N>();
                this[t] = kSet;
            }

            kSet.Add(m, n);
        }

        /// <summary>
        /// 移除指定的一级键与二级键对应项（若移除后内层字典为空，则自动将一级键也从外层字典移除）。
        /// </summary>
        public bool Remove(T t, M m)
        {
            this.TryGetValue(t, out Dictionary<M, N> dic);
            if (dic == null || !dic.Remove(m))
            {
                return false;
            }

            if (dic.Count == 0)
            {
                this.Remove(t);
            }

            return true;
        }

        /// <summary>
        /// 判断指定的一级键下是否包含指定的二级键。
        /// </summary>
        public bool ContainSubKey(T t, M m)
        {
            this.TryGetValue(t, out Dictionary<M, N> dic);
            if (dic == null)
            {
                return false;
            }

            return dic.ContainsKey(m);
        }

        /// <summary>
        /// 判断指定的一级键与二级键下是否包含指定的值。
        /// </summary>
        public bool ContainValue(T t, M m, N n)
        {
            this.TryGetValue(t, out Dictionary<M, N> dic);
            if (dic == null)
            {
                return false;
            }

            if (!dic.ContainsKey(m))
            {
                return false;
            }

            return dic.ContainsValue(n);
        }
    }
}
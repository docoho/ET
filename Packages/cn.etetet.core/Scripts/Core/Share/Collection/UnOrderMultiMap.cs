using System;
using System.Collections.Generic;

namespace ET
{
	/// <summary>
	/// 无序的一对多映射字典（继承自 <see cref="Dictionary{TKey, TValue}"/>，一个 Key 对应一个 <see cref="List{K}"/>）。
	/// <remarks>
	/// 当不需要像 <see cref="MultiMap{T, K}"/> 那样按 Key 排序时，使用此结构可以获得哈希表 O(1) 的查找和插入性能。
	/// </remarks>
	/// </summary>
	public class UnOrderMultiMap<T, K>: Dictionary<T, List<K>>
	{
		/// <summary>
		/// 向指定键 <paramref name="t"/> 下的列表追加一个元素 <paramref name="k"/>。
		/// </summary>
		public void Add(T t, K k)
		{
			List<K> list;
			this.TryGetValue(t, out list);
			if (list == null)
			{
				list = new List<K>();
				base[t] = list;
			}
			list.Add(k);
		}

		/// <summary>
		/// 从指定键 <paramref name="t"/> 下的列表中移除元素 <paramref name="k"/>（若列表变空则同时移除该键）。
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
		/// 返回内部的list（若键不存在则返回 null）。
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		public new List<K> this[T t]
		{
			get
			{
				List<K> list;
				this.TryGetValue(t, out list);
				return list;
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
			return default(K);
		}

		/// <summary>
		/// 判断指定键下的列表是否包含指定元素。
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
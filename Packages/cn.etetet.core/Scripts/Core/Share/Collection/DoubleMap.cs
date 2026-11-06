using System;
using System.Collections.Generic;

namespace ET
{
	/// <summary>
	/// 双向映射字典（Key 与 Value 一一对应，支持通过 Key O(1) 查 Value，也支持通过 Value O(1) 反查 Key）。
	/// <remarks>
	/// 内部维护正向字典 <see cref="kv"/> 与反向字典 <see cref="vk"/>，适用于协议号（Opcode）与消息类型（Type）互查等双向绑定场景。
	/// </remarks>
	/// </summary>
	/// <typeparam name="K">键类型</typeparam>
	/// <typeparam name="V">值类型</typeparam>
	public class DoubleMap<K, V>
	{
		private readonly Dictionary<K, V> kv = new();
		private readonly Dictionary<V, K> vk = new();

		public DoubleMap()
		{
		}

		public DoubleMap(int capacity)
		{
			kv = new Dictionary<K, V>(capacity);
			vk = new Dictionary<V, K>(capacity);
		}

		/// <summary>
		/// 遍历双向字典中的所有键值对并执行指定操作。
		/// </summary>
		public void ForEach(Action<K, V> action)
		{
			if (action == null)
			{
				return;
			}
			Dictionary<K, V>.KeyCollection keys = kv.Keys;
			foreach (K key in keys)
			{
				action(key, kv[key]);
			}
		}

		/// <summary>
		/// 获取所有正向键的列表副本。
		/// </summary>
		public List<K> Keys
		{
			get
			{
				return new List<K>(kv.Keys);
			}
		}

		/// <summary>
		/// 获取所有正向值的列表副本。
		/// </summary>
		public List<V> Values
		{
			get
			{
				return new List<V>(vk.Keys);
			}
		}

		/// <summary>
		/// 添加一对双向映射关系（若 Key 或 Value 已存在则抛出异常，严格保证一一对应）。
		/// </summary>
		public void Add(K key, V value)
		{
			if (key == null || value == null || kv.ContainsKey(key) || vk.ContainsKey(value))
			{
				throw new Exception($"kv error or existed: {key} {value}");
			}
			kv.Add(key, value);
			vk.Add(value, key);
		}

		/// <summary>
		/// 根据键正向查找对应的值（不存在则返回默认值）。
		/// </summary>
		public V GetValueByKey(K key)
		{
			return this.kv.GetValueOrDefault(key);
		}

		/// <summary>
		/// 根据值反向查找对应的键（不存在则返回默认值）。
		/// </summary>
		public K GetKeyByValue(V value)
		{
			return this.vk.GetValueOrDefault(value);
		}

		/// <summary>
		/// 获取底层的正向字典。
		/// </summary>
		public Dictionary<K, V> GetAll()
		{
			return this.kv;
		}

		/// <summary>
		/// 根据键移除该对双向映射关系。
		/// </summary>
		public void RemoveByKey(K key)
		{
			if (key == null)
			{
				return;
			}
			V value;
			if (!this.kv.Remove(key, out value))
			{
				return;
			}

			vk.Remove(value);
		}

		/// <summary>
		/// 根据值移除该对双向映射关系。
		/// </summary>
		public void RemoveByValue(V value)
		{
			if (value == null)
			{
				return;
			}

			K key;
			if (!vk.TryGetValue(value, out key))
			{
				return;
			}

			kv.Remove(key);
			vk.Remove(value);
		}

		/// <summary>
		/// 清空所有映射数据。
		/// </summary>
		public void Clear()
		{
			kv.Clear();
			vk.Clear();
		}

		/// <summary>
		/// 判断是否包含指定的键。
		/// </summary>
		public bool ContainsKey(K key)
		{
			if (key == null)
			{
				return false;
			}
			return kv.ContainsKey(key);
		}

		/// <summary>
		/// 判断是否包含指定的值。
		/// </summary>
		public bool ContainsValue(V value)
		{
			if (value == null)
			{
				return false;
			}
			return vk.ContainsKey(value);
		}

		/// <summary>
		/// 判断是否同时包含指定的键和值。
		/// </summary>
		public bool Contains(K key, V value)
		{
			if (key == null || value == null)
			{
				return false;
			}
			return kv.ContainsKey(key) && vk.ContainsKey(value);
		}
	}
}
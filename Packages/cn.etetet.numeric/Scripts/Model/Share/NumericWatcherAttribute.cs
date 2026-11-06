using System;

namespace ET
{
	/// <summary>
	/// 数值变化监听器特性标签（标注在实现 <see cref="INumericWatcher"/> 的类上，指定监听的场景类型与 <see cref="ET.NumericType"/>）。
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class NumericWatcherAttribute : BaseAttribute
	{
		/// <summary>
		/// 监听器生效的场景类型（<see cref="ET.SceneType"/>）。
		/// </summary>
		public int SceneType { get; }
		
		/// <summary>
		/// 监听的目标数值类型（<see cref="ET.NumericType"/>）。
		/// </summary>
		public NumericType NumericType { get; }

		/// <summary>
		/// 使用整型数值类型构造数值监听器特性。
		/// </summary>
		public NumericWatcherAttribute(int sceneType, int type)
            : this(sceneType, (NumericType)type)
		{
		}

		/// <summary>
		/// 使用 <see cref="ET.NumericType"/> 构造数值监听器特性。
		/// </summary>
		public NumericWatcherAttribute(int sceneType, NumericType type)
		{
			this.SceneType = sceneType;
			this.NumericType = type;
		}
	}
}

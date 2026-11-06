namespace ET
{
	/// <summary>
	/// 数值变化监听器接口（配合 <see cref="NumericWatcherAttribute"/> 标签声明对特定 <see cref="NumericType"/> 变化的响应逻辑）。
	/// </summary>
	public interface INumericWatcher
	{
		/// <summary>
		/// 当目标单位的指定数值发生变化时触发。
		/// </summary>
		void Run(Unit unit, NumbericChange args);
	}
}

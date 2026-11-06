namespace ET.Server
{
	/// <summary>
	/// 单位进入视野事件（单位 B 进入了单位 A 的视野范围）。
	/// </summary>
	public struct UnitEnterSightRange
	{
		/// <summary>
		/// 观察者单位 A。
		/// </summary>
		public EntityRef<Unit> A;

		/// <summary>
		/// 进入 A 视野的目标单位 B。
		/// </summary>
		public EntityRef<Unit> B;
	}

	/// <summary>
	/// 单位离开视野事件（单位 B 离开了单位 A 的视野范围）。
	/// </summary>
	public struct UnitLeaveSightRange
	{
		/// <summary>
		/// 观察者单位 A。
		/// </summary>
		public EntityRef<Unit> A;

		/// <summary>
		/// 离开 A 视野的目标单位 B。
		/// </summary>
		public EntityRef<Unit> B;
	}
}
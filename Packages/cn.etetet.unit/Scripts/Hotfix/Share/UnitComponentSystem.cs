namespace ET
{
	/// <summary>
	/// <see cref="UnitComponent"/> 实体管理组件系统，提供 Unit 的添加、按 ID 查询与移除方法。
	/// </summary>
	public static partial class UnitComponentSystem
	{
		/// <summary>
		/// 将 Unit 添加到管理组件中（Unit 已通过 AddChild 挂载为子实体，此处预留扩展钩子）。
		/// </summary>
		public static void Add(this UnitComponent self, Unit unit)
		{
		}

		/// <summary>
		/// 根据实体 ID 获取对应的 <see cref="Unit"/> 子实体。
		/// </summary>
		public static Unit Get(this UnitComponent self, long id)
		{
			Unit unit = self.GetChild<Unit>(id);
			return unit;
		}

		/// <summary>
		/// 根据实体 ID 移除对应的 <see cref="Unit"/> 子实体（可指定是否同时调用 Dispose 销毁实体）。
		/// </summary>
		public static void Remove(this UnitComponent self, long id, bool isDispose = true)
		{
			self.RemoveChild(id, isDispose);
		}
	}
}
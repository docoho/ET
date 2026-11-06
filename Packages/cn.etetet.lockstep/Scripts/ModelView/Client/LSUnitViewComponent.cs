namespace ET.Client
{
	/// <summary>
	/// 帧同步房间内所有单位视图（<see cref="LSUnitView"/>）的管理容器组件
	/// </summary>
	[ComponentOf(typeof(Room))]
	public class LSUnitViewComponent: Entity, IAwake, IDestroy
	{
		/// <summary>
		/// 本地玩家控制的单位视图引用
		/// </summary>
		public EntityRef<LSUnitView> myUnitView;
	}
}
namespace ET
{
	/// <summary>
	/// 场景单位管理组件（挂载在 Map 或客户端 CurrentScene 上，以子实体方式管理场景内的所有 <see cref="Unit"/>）。
	/// </summary>
	[ComponentOf(typeof(Scene))]
	public class UnitComponent: Entity, IAwake, IDestroy
	{
	}
}
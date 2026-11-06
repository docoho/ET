namespace ET
{
	/// <summary>
	/// 跟随父实体一起序列化（存档/传输）的标记接口。
	/// <para>
	/// 【初学者必读】
	/// 默认情况下，挂载在父实体（如 <c>Unit</c>）上的组件（<c>Components</c>）和子实体（<c>Children</c>）在父实体序列化时<b>不会被自动包含</b>（因为很多组件只是运行时临时状态，如寻路缓存、视野组件）。
	/// 只有当组件或子实体显式实现了 <see cref="ISerializeToEntity"/> 接口时，它在挂载时才会自动将 <see cref="Entity.IsSerializeWithParent"/> 设为 <c>true</c>，从而跟随父实体一起序列化到数据库或网络包中！
	/// </para>
	/// </summary>
	public interface ISerializeToEntity
	{
	}
}
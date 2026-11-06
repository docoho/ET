using MemoryPack;

namespace ET
{
	/// <summary>
	/// 帧同步世界单位管理容器组件（挂载在 <see cref="LSWorld"/> 上，所有 <see cref="LSUnit"/> 均作为其子实体并随世界快照一起序列化）
	/// </summary>
	[ComponentOf(typeof(LSWorld))]
	[MemoryPackable]
	public partial class LSUnitComponent: LSEntity, IAwake, ISerializeToEntity
	{
	}
}
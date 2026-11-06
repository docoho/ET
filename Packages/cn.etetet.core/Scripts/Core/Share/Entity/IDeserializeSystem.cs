using System;

namespace ET
{
	/// <summary>
	/// 实体反序列化回调标记接口。
	/// 当一个实体从数据库（MongoDB）或网络字节流（MemoryPack）反序列化出来后，因为不会走 <c>AddComponent</c> 的 <c>Awake</c>，
	/// 若需要在它重新挂载到场景树（设置 <see cref="Entity.IScene"/>）时重建运行时索引缓存，请让实体实现此接口并编写对应的 <c>Deserialize</c> System 方法。
	/// </summary>
	public interface IDeserialize
	{
	}
	
	/// <summary>
	/// 反序列化系统内部调度接口。
	/// </summary>
	public interface IDeserializeSystem: ISystemType
	{
		void Run(Entity o);
	}

	/// <summary>
	/// 反序列化后执行的 System 抽象基类（由 SourceGenerator 自动生成派生类）。
	/// </summary>
	/// <typeparam name="T">目标实体类型</typeparam>
	[EntitySystem]
	public abstract class DeserializeSystem<T> : SystemObject, IDeserializeSystem where T: Entity, IDeserialize
	{
		void IDeserializeSystem.Run(Entity o)
		{
			this.Deserialize((T)o);
		}

		Type ISystemType.SystemType()
		{
			return typeof(IDeserializeSystem);
		}

		Type ISystemType.Type()
		{
			return typeof(T);
		}

		protected abstract void Deserialize(T self);
	}
}

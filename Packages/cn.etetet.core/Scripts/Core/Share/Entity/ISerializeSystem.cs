using System;

namespace ET
{
	/// <summary>
	/// 实体序列化前置回调标记接口（当调用 <see cref="Entity.BeginInit"/> 准备序列化时触发）。
	/// </summary>
	public interface ISerialize
	{
	}
	
	/// <summary>
	/// 序列化前置系统内部调度接口。
	/// </summary>
	public interface ISerializeSystem: ISystemType
	{
		void Run(Entity o);
	}

	/// <summary>
	/// 序列化前执行的 System 抽象基类（由 SourceGenerator 自动生成派生类）。
	/// </summary>
	/// <typeparam name="T">目标实体类型</typeparam>
	[EntitySystem]
	public abstract class SerializeSystem<T> : SystemObject, ISerializeSystem where T: Entity, ISerialize
	{
		void ISerializeSystem.Run(Entity o)
		{
			this.Serialize((T)o);
		}

		Type ISystemType.SystemType()
		{
			return typeof(ISerializeSystem);
		}

		Type ISystemType.Type()
		{
			return typeof(T);
		}

		protected abstract void Serialize(T self);
	}
}

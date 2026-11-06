using System;

namespace ET
{
	/// <summary>
	/// 实体销毁清理标记接口。
	/// 【初学者必读】因为 ET 的 Entity 默认支持对象池复用，所以在 Entity 类上声明的任何字段（如字典、列表、定时器 ID、引用等），
	/// 都必须实现 <see cref="IDestroy"/> 接口并在对应的 <c>Destroy(this T self)</c> System 方法中彻底清空/重置，否则下次从对象池取出时会残留上一次的脏数据！
	/// </summary>
	public interface IDestroy
	{
	}
	
	/// <summary>
	/// 销毁系统内部调度接口。
	/// </summary>
	public interface IDestroySystem: ISystemType
	{
		void Run(Entity o);
	}

	/// <summary>
	/// 实体销毁（<see cref="Entity.Dispose"/>）时执行的 System 抽象基类（由 SourceGenerator 自动生成派生类）。
	/// </summary>
	[EntitySystem]
	public abstract class DestroySystem<T> : SystemObject, IDestroySystem where T: Entity, IDestroy
	{
		void IDestroySystem.Run(Entity o)
		{
			this.Destroy((T)o);
		}

		Type ISystemType.SystemType()
		{
			return typeof(IDestroySystem);
		}

		Type ISystemType.Type()
		{
			return typeof(T);
		}

		protected abstract void Destroy(T self);
	}
}

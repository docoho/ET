using System;

namespace ET
{
	/// <summary>
	/// 实体组件获取拦截标记接口。
	/// <para>
	/// 【设计意图】
	/// 当一个父实体（如 <c>Unit</c>）实现了 <see cref="IGetComponentSys"/> 接口后，每次外部调用 <c>unit.GetComponent&lt;K&gt;()</c> 时，
	/// 都会先触发 <see cref="IGetComponentSysSystem"/>。这可以用于按需懒加载组件，或者记录哪些组件被访问/修改过（脏标记增量存库优化）。
	/// </para>
	/// </summary>
	// GetComponentSystem有巨大作用，比如每次保存Unit的数据不需要所有组件都保存，只需要保存Unit变化过的组件
	// 是否变化可以通过判断该组件是否GetComponent，Get了就记录该组件
	// 这样可以只保存Unit变化过的组件
	// 再比如传送也可以做此类优化
	public interface IGetComponentSys
	{
	}
	
	/// <summary>
	/// GetComponent 拦截系统内部调度接口。
	/// </summary>
	public interface IGetComponentSysSystem: ISystemType
	{
		void Run(Entity o, Type type);
	}

	/// <summary>
	/// GetComponent 拦截系统抽象基类（由 SourceGenerator 自动生成派生类）。
	/// </summary>
	[EntitySystem]
	public abstract class GetComponentSysSystem<T> : SystemObject, IGetComponentSysSystem where T: Entity, IGetComponentSys
	{
		void IGetComponentSysSystem.Run(Entity o, Type type)
		{
			this.GetComponentSys((T)o, type);
		}

		Type ISystemType.SystemType()
		{
			return typeof(IGetComponentSysSystem);
		}

		Type ISystemType.Type()
		{
			return typeof(T);
		}

		protected abstract void GetComponentSys(T self, Type type);
	}
}

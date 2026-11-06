using System;

namespace ET
{
	/// <summary>
	/// LateUpdate 帧末更新事件结构体标识。
	/// </summary>
	public struct LateUpdateEvent
	{
	}
	
	/// <summary>
	/// 实体 LateUpdate 生命周期标记接口（在每帧所有 Update 执行完毕后调用，常用于相机跟随、UI 位置同步等逻辑）。
	/// </summary>
	public interface ILateUpdate: IEvent<LateUpdateEvent>
	{
	}

	/// <summary>
	/// LateUpdate 系统抽象基类（由 SourceGenerator 自动生成派生类，在 <see cref="Fiber.LateUpdate"/> 中驱动）。
	/// </summary>
	[EntitySystem]
	public abstract class LateUpdateSystem<T> : EventSystem<T, LateUpdateEvent> where T: Entity, ILateUpdate
	{
		protected override void Event(T e, LateUpdateEvent t)
		{
			this.LateUpdate(e);
		}

		protected abstract void LateUpdate(T self);
	}
}

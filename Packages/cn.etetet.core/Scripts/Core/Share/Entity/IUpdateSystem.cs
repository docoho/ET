using System;

namespace ET
{
	/// <summary>
	/// Update 每帧更新事件结构体标识。
	/// </summary>
	public struct UpdateEvent
	{
	}
	
	/// <summary>
	/// 实体每帧更新（Update）生命周期标记接口。
	/// 【性能提示】在 ET 中，尽量使用 <see cref="TimerComponent"/> 定时器或事件驱动代替每帧轮询的 <see cref="IUpdate"/>，只有真正需要每帧计算的组件才应实现此接口。
	/// </summary>
	public interface IUpdate: IEvent<UpdateEvent>
	{
	}

	/// <summary>
	/// Update 系统抽象基类（由 SourceGenerator 自动生成派生类，在 <see cref="Fiber.Update"/> 中驱动）。
	/// </summary>
	[EntitySystem]
	public abstract class UpdateSystem<T> : EventSystem<T, UpdateEvent> where T: Entity, IUpdate
	{
		protected override void Event(T e, UpdateEvent t)
		{
			this.Update(e);
		}

		protected abstract void Update(T self);
	}
}

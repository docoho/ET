using System;

namespace ET
{
	/// <summary>
	/// 业务事件订阅处理器基础接口。
	/// </summary>
	public interface IEvent
	{
		/// <summary>所监听的事件参数结构体类型</summary>
		Type Type { get; }
	}
	
	/// <summary>
	/// 跨模块业务事件订阅处理器泛型抽象基类（需配合 <see cref="EventAttribute"/> 特性使用）。
	/// </summary>
	/// <typeparam name="S">触发事件的场景类型（通常为 <see cref="Scene"/>）</typeparam>
	/// <typeparam name="A">事件参数值类型结构体（struct）</typeparam>
	public abstract class AEvent<S, A>: IEvent where S: class, IScene where A: struct
	{
		public Type Type
		{
			get
			{
				return typeof (A);
			}
		}

		/// <summary>
		/// 子类重写的事件处理逻辑入口。
		/// </summary>
		protected abstract ETTask Run(S scene, A a);

		/// <summary>
		/// 框架内部调度入口（自动包裹 try-catch 防止单个事件订阅者抛异常中断其他订阅者）。
		/// </summary>
		public async ETTask Handle(S scene, A a)
		{
			try
			{
				await Run(scene, a);
			}
			catch (Exception e)
			{
				Log.Error(e);
			}
		}
	}
}
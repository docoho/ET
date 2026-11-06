namespace ET
{
	/// <summary>
	/// 监听 <see cref="NumbericChange"/> 事件并将其转发给 <see cref="NumericWatcherComponent"/> 分发给具体的属性监听器。
	/// </summary>
	// 分发数值监听
	[Event(SceneType.All)]  // 服务端Map需要分发, 客户端CurrentScene也要分发
	public class NumericChangeEvent_NotifyWatcher: AEvent<Scene, NumbericChange>
	{
		/// <summary>
		/// 触发数值监听组件的分发执行。
		/// </summary>
		protected override async ETTask Run(Scene scene, NumbericChange args)
		{
			NumericWatcherComponent.Instance.Run(args.Unit, args);
			await ETTask.CompletedTask;
		}
	}
}

using System;

namespace ET
{
	/// <summary>
	/// 跨模块业务事件订阅处理器标记特性（配合 <see cref="AEvent{S,A}"/> 派生类使用）。
	/// </summary>
	/// <remarks>
	/// 通过指定 <see cref="SceneType"/>，可以精确限制该事件处理器只在特定类型的场景（如 <c>SceneType.Current</c>、<c>SceneType.Map</c> 或 <c>SceneType.All</c>）抛事件时才响应！
	/// </remarks>
	public class EventAttribute: BaseAttribute
	{
		/// <summary>该事件处理器监听的目标场景类型（若为 <c>SceneType.All = 0</c> 则响应所有场景抛出的该事件）</summary>
		public int SceneType { get; }

		public EventAttribute(int sceneType)
		{
			this.SceneType = sceneType;
		}
	}
}
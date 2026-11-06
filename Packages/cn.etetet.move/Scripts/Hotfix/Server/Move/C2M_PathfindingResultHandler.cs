
using System;

namespace ET.Server
{
	/// <summary>
	/// 服务端处理客户端寻路移动请求（C2M_PathfindingResult）的位置消息处理器。
	/// </summary>
	[MessageHandler(SceneType.Map)]
	public class C2M_PathfindingResultHandler : MessageLocationHandler<Unit, C2M_PathfindingResult>
	{
		/// <summary>
		/// 触发单位向目标坐标寻路并移动。
		/// </summary>
		protected override async ETTask Run(Unit unit, C2M_PathfindingResult message)
		{
			unit.FindPathMoveToAsync(message.Position).Coroutine();
			
			await ETTask.CompletedTask;
		}
	}
}
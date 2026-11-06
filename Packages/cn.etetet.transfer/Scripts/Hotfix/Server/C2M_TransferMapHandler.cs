using System;

namespace ET.Server
{
	/// <summary>
	/// 客户端请求跨地图传送（C2M_TransferMap）的消息处理器（示例：在 Map1 与 Map2 之间来回切换）。
	/// </summary>
	[MessageHandler(SceneType.Map)]
	public class C2M_TransferMapHandler : MessageLocationHandler<Unit, C2M_TransferMap, M2C_TransferMap>
	{
		/// <summary>
		/// 根据当前地图名称决定目标地图，并在当前帧结束时触发跨场景传送流程。
		/// </summary>
		protected override async ETTask Run(Unit unit, C2M_TransferMap request, M2C_TransferMap response)
		{
			string currentMap = unit.Scene().Name.GetSceneConfigName();
			string toMap = null;
			if (currentMap == "Map1")
			{
				toMap = "Map2";
			}
			else
			{
				toMap = "Map1";
			}
			
			// 等待当前帧逻辑执行完毕后再执行传送，避免在消息处理中途销毁或迁移当前 Unit
			TransferHelper.TransferAtFrameFinish(unit, toMap, 0).Coroutine();
			
			await ETTask.CompletedTask;
		}
	}
}
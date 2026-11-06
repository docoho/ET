namespace ET.Server
{
	/// <summary>
	/// Gate 请求让玩家进入地图（G2Map_EnterMap）的消息处理器。
	/// </summary>
	[MessageHandler(SceneType.Map)]
	public class G2Map_EnterMapHandler : MessageHandler<Scene, G2Map_EnterMap, Map2G_EnterMap>
	{
		/// <summary>
		/// 创建玩家的初始 Unit 并绑定 Gate 会话 ActorId，在当前帧结束后通过 <see cref="TransferHelper"/> 传送至目标地图副本。
		/// </summary>
		protected override async ETTask Run(Scene scene, G2Map_EnterMap request, Map2G_EnterMap response)
		{
			// 这里可以从DB中加载Unit
			Unit unit = UnitFactory.Create(scene, request.PlayerId, request.UnitConfigId);
			unit.AddComponent<UnitGateInfoComponent>().ActorId = request.GateActorId;

			EntityRef<Unit> unitRef = unit;
			await scene.Fiber().WaitFrameFinish();

			unit = unitRef;
			await TransferHelper.TransferLock(unit, request.MapName, request.MapId, true);
		}
	}
}

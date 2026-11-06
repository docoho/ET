namespace ET.Server
{
	/// <summary>
	/// 客户端请求从网关（Gate）进入地图场景（C2G_EnterMap）的消息处理器。
	/// </summary>
	[MessageSessionHandler(SceneType.Gate)]
	public class C2G_EnterMapHandler : MessageSessionHandler<C2G_EnterMap, G2C_EnterMap>
	{
		protected override async ETTask Run(Session session, C2G_EnterMap request, G2C_EnterMap response)
		{
			Player player = session.GetComponent<SessionPlayerComponent>().Player;
			EntityRef<Player> playerRef = player;
			// 为玩家创建一个临时中转用的 GateMap 纤程
			GateMapComponent gateMapComponent = player.AddComponent<GateMapComponent>();
			EntityRef<GateMapComponent> gateMapComponentRef = gateMapComponent;
			await gateMapComponent.Create(player.Id);

			player = playerRef;
			gateMapComponent = gateMapComponentRef;
			response.MyId = player.Id;

			// 等到一帧的最后面再进图，先让G2C_EnterMap返回，否则切场景消息可能比G2C_EnterMap还早
			EnterMapAtFrameFinish(player, gateMapComponent, player.GetComponent<PlayerSessionComponent>().GetActorId(), "Map2", 0, 1001).Coroutine();
		}

		/// <summary>
		/// 在当前帧末尾向临时 GateMap 纤程发起进图 RPC，传送完成后清理临时 GateMap 纤程。
		/// </summary>
		private static async ETTask EnterMapAtFrameFinish(Player player, GateMapComponent gateMapComponent, ActorId gateActorId, string mapName, int mapId, int unitConfigId)
		{
			EntityRef<Player> playerRef = player;
			EntityRef<GateMapComponent> gateMapComponentRef = gateMapComponent;
			await player.Fiber().WaitFrameFinish();

			player = playerRef;
			gateMapComponent = gateMapComponentRef;

			G2Map_EnterMap g2MapEnterMap = G2Map_EnterMap.Create();
			g2MapEnterMap.PlayerId = player.Id;
			g2MapEnterMap.GateActorId = gateActorId;
			g2MapEnterMap.MapName = mapName;
			g2MapEnterMap.MapId = mapId;
			g2MapEnterMap.UnitConfigId = unitConfigId;

			try
			{
				await player.Root().GetComponent<MessageSender>().Call(gateMapComponent.Fiber.Root.GetActorId(), g2MapEnterMap);
			}
			finally
			{
				player = playerRef;
				gateMapComponent = gateMapComponentRef;
				await player.Fiber().RemoveFiber(gateMapComponent.Fiber.Id);
				player = playerRef;
				player.RemoveComponent<GateMapComponent>();
			}
		}
	}
}

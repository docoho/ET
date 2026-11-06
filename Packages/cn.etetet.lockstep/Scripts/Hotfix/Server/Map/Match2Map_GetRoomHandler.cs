using System;
using System.Collections.Generic;

namespace ET.Server
{
	/// <summary>
	/// 匹配服向 Map 服请求分配并创建帧同步房间（<see cref="Match2Map_GetRoom"/>）的处理器
	/// </summary>
	[MessageHandler(SceneType.Map)]
	public class Match2Map_GetRoomHandler : MessageHandler<Scene, Match2Map_GetRoom, Map2Match_GetRoom>
	{
		/// <summary>
		/// 动态创建一个独立的 <see cref="SceneType.RoomRoot"/> 线程池纤程，调用 <see cref="RoomManager2Room_Init"/> 完成房间初始化后返回房间 ActorId
		/// </summary>
		/// <param name="root">Map 服根场景</param>
		/// <param name="request">创建房间请求（包含参赛玩家 ID 列表）</param>
		/// <param name="response">创建房间响应（包含新建房间的 ActorId）</param>
		protected override async ETTask Run(Scene root, Match2Map_GetRoom request, Map2Match_GetRoom response)
		{
			//RoomManagerComponent roomManagerComponent = root.GetComponent<RoomManagerComponent>();
			
			Fiber fiber = root.Fiber();

			EntityRef<Scene> rootRef = root;
			
			long fiberId = await fiber.CreateFiber(SchedulerType.ThreadPool, IdGenerater.Instance.GenerateId(), SceneType.RoomRoot, "RoomRoot");
			ActorId roomRootActorId = new(fiber.Process, fiberId);

			// 发送消息给房间纤程，初始化
			RoomManager2Room_Init roomManager2RoomInit = RoomManager2Room_Init.Create();
			roomManager2RoomInit.PlayerIds.AddRange(request.PlayerIds);
			root = rootRef;
			await root.GetComponent<MessageSender>().Call(roomRootActorId, roomManager2RoomInit);
			
			response.ActorId = roomRootActorId;
			await ETTask.CompletedTask;
		}
	}
}

using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 房间管理器向新创建的房间纤程发送初始化请求（<see cref="RoomManager2Room_Init"/>）的处理器
    /// </summary>
    [MessageHandler(SceneType.RoomRoot)]
    public class RoomManager2Room_InitHandler: MessageHandler<Scene, RoomManager2Room_Init, Room2RoomManager_Init>
    {
        /// <summary>
        /// 初始化服务端房间实体 <see cref="Room"/>、挂载 <see cref="RoomServerComponent"/> 记录参赛玩家列表，并创建服务端帧同步世界 <see cref="LSWorld"/>
        /// </summary>
        /// <param name="root">房间纤程根场景</param>
        /// <param name="request">房间初始化请求（含匹配成功的玩家 ID 列表）</param>
        /// <param name="response">房间初始化响应</param>
        protected override async ETTask Run(Scene root, RoomManager2Room_Init request, Room2RoomManager_Init response)
        {
            Room room = root.AddComponent<Room>();
            room.Name = "Server";
            room.AddComponent<RoomServerComponent, List<long>>(request.PlayerIds);

            room.LSWorld = new LSWorld(SceneType.LockStepServer);
            await ETTask.CompletedTask;
        }
    }
}
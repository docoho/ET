namespace ET.Server
{
    /// <summary>
    /// 处理 Map 通知 MapManager 玩家下线离开地图（Map2MapManager_LogoutRequest）的消息处理器。
    /// </summary>
    [MessageHandler(SceneType.MapManager)]
    public class Map2MapManager_LogoutRequestHandler: MessageHandler<Scene, Map2MapManager_LogoutRequest, Map2MapManager_LogoutResponse>
    {
        /// <summary>
        /// 将下线玩家从对应地图副本的在线玩家集合中移除。
        /// </summary>
        protected override async ETTask Run(Scene root, Map2MapManager_LogoutRequest request, Map2MapManager_LogoutResponse response)
        {
            MapManagerComponent mapManagerComponent = root.GetComponent<MapManagerComponent>();
            MapCopy mapCopy = mapManagerComponent.GetMap(request.MapName, request.MapId);
            if (mapCopy == null)
            {
                return;
            }

            mapCopy.Players.Remove(request.UnitId);
            
            await ETTask.CompletedTask;
        }
    }
}
